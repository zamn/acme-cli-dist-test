import { FernApiClient } from '@zamn/acme-uat-sdk';

(async () => {
  const client = new FernApiClient({});

  const data = await client.widgets.listWidgets();

  console.log(data);
})();
