import { FernApiEnvironment } from './http/environment';
import {
  BaseClientOptions,
  BaseRequestOptions,
  NormalizedClientOptions,
  core,
  normalizeClientOptions,
} from './http/types';
import { Supplier } from './http/utils/supplier';
import { makePassthroughRequest } from './http/utils/passthrough-request';
import { WidgetsClient } from './api/resources/widgets';
import { GadgetsClient } from './api/resources/gadgets';

export declare namespace FernApiClient {
  export type Options = BaseClientOptions;
  export interface RequestOptions extends BaseRequestOptions {}
}

export class FernApiClient {
  protected readonly _options: NormalizedClientOptions<BaseClientOptions>;

  protected _widgets: WidgetsClient | undefined;

  protected _gadgets: GadgetsClient | undefined;

  constructor(options: BaseClientOptions = {}) {
    this._options = normalizeClientOptions(options);
  }

  public get widgets(): WidgetsClient {
    return (this._widgets ??= new WidgetsClient(this._options));
  }

  public get gadgets(): GadgetsClient {
    return (this._gadgets ??= new GadgetsClient(this._options));
  }

  /**
   * Escape hatch for calling an endpoint this SDK doesn't wrap: resolves `input` against the
   * configured baseUrl/environment, applies the SDK's default headers, auth, timeout, and retry
   * policy, and returns the raw `Response` — no request/response schema validation.
   */
  public async fetch(
    input: Request | string | URL,
    init?: RequestInit,
    requestOptions?: core.PassthroughRequest.RequestOptions,
  ): Promise<Response> {
    return makePassthroughRequest(
      input,
      init,
      {
        baseUrl: this._options.baseUrl ?? this._options.environment,
        headers: this._options.headers,
        timeoutInSeconds: this._options.timeoutInSeconds,
        maxRetries: this._options.maxRetries,
        fetch: this._options.fetch,
        logging: this._options.logging,
      },
      requestOptions,
    );
  }
}
