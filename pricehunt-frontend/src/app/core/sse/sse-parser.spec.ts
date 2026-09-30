import { SseParser, type SseMessage } from './sse-parser';

function parseAll(...chunks: string[]): SseMessage[] {
  const parser = new SseParser();
  return chunks.flatMap((chunk) => parser.push(chunk));
}

describe('SseParser', () => {
  it('dispatches an event at the blank line that ends it', () => {
    const parser = new SseParser();

    expect(parser.push('event: quote-received\ndata: {"a":1}\n')).toEqual([]);
    expect(parser.push('\n')).toEqual([{ event: 'quote-received', data: '{"a":1}', id: '' }]);
  });

  it('defaults the event type to "message"', () => {
    expect(parseAll('data: hello\n\n')).toEqual([{ event: 'message', data: 'hello', id: '' }]);
  });

  it.each([
    ['LF', '\n'],
    ['CRLF', '\r\n'],
    ['CR', '\r'],
  ])('accepts %s line endings', (_name, eol) => {
    const stream = `event: a${eol}data: 1${eol}${eol}event: b${eol}data: 2${eol}${eol}`;

    expect(parseAll(stream).map((message) => [message.event, message.data])).toEqual([
      ['a', '1'],
      ['b', '2'],
    ]);
  });

  it('handles a stream split at every possible position', () => {
    const stream =
      'id: 7\r\nevent: search-started\r\ndata: {"searchId":"x"}\r\n\r\n: keep-alive\n\ndata: tail\n\n';
    const expected = parseAll(stream);

    for (let split = 1; split < stream.length; split++) {
      expect(parseAll(stream.slice(0, split), stream.slice(split))).toEqual(expected);
    }
    expect(expected).toEqual([
      { event: 'search-started', data: '{"searchId":"x"}', id: '7' },
      { event: 'message', data: 'tail', id: '7' },
    ]);
  });

  it('does not count a CRLF split across chunks as two line endings', () => {
    expect(parseAll('data: a\r', '\n\r', '\n')).toEqual([{ event: 'message', data: 'a', id: '' }]);
  });

  it('joins multi-line data with newlines', () => {
    expect(parseAll('data: first\ndata:second\ndata\n\n')).toEqual([
      { event: 'message', data: 'first\nsecond\n', id: '' },
    ]);
  });

  it('ignores comments and unknown fields', () => {
    expect(parseAll(': comment\nfoo: bar\ndata: x\n\n')).toEqual([
      { event: 'message', data: 'x', id: '' },
    ]);
  });

  it('keeps the last event id across events and ignores ids containing NULL', () => {
    expect(
      parseAll('id: 1\ndata: a\n\ndata: b\n\nid: 2\u0000\ndata: c\n\n').map(
        (message) => message.id,
      ),
    ).toEqual(['1', '1', '1']);
  });

  it('reads a numeric retry field and ignores other values', () => {
    expect(parseAll('retry: 3000\ndata: a\n\nretry: soon\ndata: b\n\n')).toEqual([
      { event: 'message', data: 'a', id: '', retry: 3000 },
      { event: 'message', data: 'b', id: '' },
    ]);
  });

  it('does not dispatch an event without data', () => {
    expect(parseAll('event: ping\n\ndata: x\n\n')).toEqual([
      { event: 'message', data: 'x', id: '' },
    ]);
  });

  it('strips only one space after the colon', () => {
    expect(parseAll('data:  two spaces\n\n')[0]?.data).toBe(' two spaces');
  });

  it('ignores a byte order mark at the start of the stream', () => {
    expect(parseAll('\uFEFFdata: x\n\n')).toEqual([{ event: 'message', data: 'x', id: '' }]);
  });

  it('never dispatches a trailing partial event', () => {
    expect(parseAll('data: complete\n\ndata: partial\n')).toEqual([
      { event: 'message', data: 'complete', id: '' },
    ]);
  });
});
