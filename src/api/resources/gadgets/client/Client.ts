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
import { ListGadgetsResponse, listGadgetsResponseResponse } from '../types/ListGadgetsResponse';

export declare namespace GadgetsClient {
  export type Options = Partial<BaseClientOptions>;
  export interface RequestOptions extends BaseRequestOptions {}
}

/**
 * Client for Gadgets operations.
 * Provides methods to interact with Gadgets-related API endpoints.
 * All methods return promises and handle request/response serialization automatically.
 */
export class GadgetsClient extends BaseService {
  protected listGadgetsConfig?: Partial<BaseClientOptions>;

  /**
   * Sets method-level configuration for listGadgets.
   * @param config - Partial configuration to override service-level defaults
   * @returns This service instance for method chaining
   */
  protected setListGadgetsConfig(config: Partial<BaseClientOptions>): this {
    this.listGadgetsConfig = config;
    return this;
  }

  /**
   *
   * @param {GadgetsClient.RequestOptions} [requestOptions] - Request-specific configuration.
   * @returns {HttpResponsePromise<FernApi.ListGadgetsResponse>} - OK
   *
   * @throws {@link errors.FernApiError}
   * @throws {@link errors.FernApiTimeoutError}
   *
   * @example
   *     await client.gadgets.listGadgets()
   */
  listGadgets(
    requestOptions?: GadgetsClient.RequestOptions,
  ): HttpResponsePromise<FernApi.ListGadgetsResponse> {
    return HttpResponsePromise.fromPromise(this.__listGadgets(requestOptions));
  }
  private async __listGadgets(
    requestOptions?: GadgetsClient.RequestOptions,
  ): Promise<WithRawResponse<FernApi.ListGadgetsResponse>> {
    const resolvedConfig = this.getResolvedConfig(this.listGadgetsConfig, requestOptions);
    const resolvedBaseUrl = await Supplier.get(
      resolvedConfig.baseUrl ?? resolvedConfig.environment,
    );
    const resolvedHeaders = await resolveHeaders(resolvedConfig.headers);
    const _request = new RequestBuilder()
      .setConfig({ ...resolvedConfig, headers: resolvedHeaders })
      .setBaseUrl(resolvedBaseUrl)
      .setMethod('GET')
      .setPath('/gadgets')
      .setRequestSchema(core.cast.identity<unknown>())
      .setRequestContentType(ContentType.Json)
      .addResponse({
        schema: listGadgetsResponseResponse,
        contentType: ContentType.Json,
        status: 200,
      })
      .build();
    return this.client.callWithRawResponse<FernApi.ListGadgetsResponse>(_request);
  }
}
