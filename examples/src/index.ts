import { FernApiClient } from '@zamn/acme-prodval-ts';

(async () => {
  const client = new FernApiClient({});

  const data = await client.widgets.listWidgets();

  console.log(data);
})();
