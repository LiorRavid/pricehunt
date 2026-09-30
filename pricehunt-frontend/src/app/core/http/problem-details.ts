/** An RFC 9457 problem, as the API returns it for failed requests. */
export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  /** Field errors of a validation problem, keyed by request field. */
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

/** A request the server answered with an error status. */
export class HttpProblemError extends Error {
  constructor(
    readonly status: number,
    readonly problem: ProblemDetails,
  ) {
    super(problem.title ?? `The request failed with status ${String(status)}.`);
    this.name = 'HttpProblemError';
  }

  /** Reads the problem from an error response, tolerating bodies that aren't problem JSON. */
  static async fromResponse(response: Response): Promise<HttpProblemError> {
    const contentType = response.headers.get('content-type') ?? '';
    const problem = contentType.includes('json')
      ? await response.json().then(
          (body: unknown) => (isProblemDetails(body) ? body : {}),
          () => ({}),
        )
      : {};
    return new HttpProblemError(response.status, problem);
  }
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
