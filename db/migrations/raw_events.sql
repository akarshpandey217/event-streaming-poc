CREATE SCHEMA IF NOT EXISTS retail;

CREATE TABLE IF NOT EXISTS retail.raw_events(
    event_id uuid PRIMARY KEY,
    tenant_id text NOT NULL,
    event_type text NOT NULL,
    session_id text NOT NULL,
    user_id text NOT NULL,
    campaign_id text,
    product_id text,
    occurred_at timestamptz NOT NULL,
    received_at timestamptz NOT NULL DEFAULT now()
);

CREATE index if not exists ix_raw_events_tenant_campaign on retail.raw_events
(tenant_id, campaign_id, event_type)