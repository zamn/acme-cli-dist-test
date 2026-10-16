import * as core from '../../../../core';
import type * as FernApi from '../../..';
import { BaseService } from '../../../../services/base-service';
import {
  BaseClientOptions,
  BaseRequestOptions,
  ContentType,
  HttpResponse,
} from '../../../../http/types';
import { RequestBuilder } from '../../../../http/transport/request-builder';
import { SerializationStyle } from '../../../../http/serialization/base-serializer';
import { FernApiError } from '../../../../http/errors/throwable-error';
import { HttpResponsePromise, WithRawResponse } from '../../../../http/response-promise';
import { Supplier, resolveHeaders } from '../../../../http/utils/supplier';
import { FernApiEnvironment } from '../../../../http/environment';
import { ListWidgetsResponse, listWidgetsResponseResponse } from '../types/ListWidgetsResponse';

export declare namespace WidgetsClient {
  export type Options = Partial<BaseClientOptions>;
  export interface RequestOptions extends BaseRequestOptions {}
}

/**
 * Client for Widgets operations.
 * Provides methods to interact with Widgets-related API endpoints.
 * All methods return promises and handle request/response serialization automatically.
 */
export class WidgetsClient extends BaseService {
  protected listWidgetsConfig?: Partial<BaseClientOptions>;

  /**
   * Sets method-level configuration for listWidgets.
   * @param config - Partial configuration to override service-level defaults
   * @returns This service instance for method chaining
   */
  protected setListWidgetsConfig(config: Partial<BaseClientOptions>): this {
    this.listWidgetsConfig = config;
    return this;
  }

  /**
   *
   * @param {WidgetsClient.RequestOptions} [requestOptions] - Request-specific configuration.
   * @returns {HttpResponsePromise<FernApi.ListWidgetsResponse>} - OK
   *
   * @throws {@link errors.FernApiError}
   * @throws {@link errors.FernApiTimeoutError}
   *
   * @example
   *     await client.widgets.listWidgets()
   */
  listWidgets(
    requestOptions?: WidgetsClient.RequestOptions,
  ): HttpResponsePromise<FernApi.ListWidgetsResponse> {
    return HttpResponsePromise.fromPromise(this.__listWidgets(requestOptions));
  }
  private async __listWidgets(
    requestOptions?: WidgetsClient.RequestOptions,
  ): Promise<WithRawResponse<FernApi.ListWidgetsResponse>> {
    const resolvedConfig = this.getResolvedConfig(this.listWidgetsConfig, requestOptions);
    const resolvedBaseUrl = await Supplier.get(
      resolvedConfig.baseUrl ?? resolvedConfig.environment,
    );
    const resolvedHeaders = await resolveHeaders(resolvedConfig.headers);
    const _request = new RequestBuilder()
      .setConfig({ ...resolvedConfig, headers: resolvedHeaders })
      .setBaseUrl(resolvedBaseUrl)
      .setMethod('GET')
      .setPath('/widgets')
      .setRequestSchema(core.cast.identity<unknown>())
      .setRequestContentType(ContentType.Json)
      .addResponse({
        schema: listWidgetsResponseResponse,
        contentType: ContentType.Json,
        status: 200,
      })
      .build();
    return this.client.callWithRawResponse<FernApi.ListWidgetsResponse>(_request);
  }
}
