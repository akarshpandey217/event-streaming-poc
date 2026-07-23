CREATE SCHEMA IF NOT EXISTS event_streaming_poc;

CREATE TABLE IF NOT EXISTS event_streaming_poc.raw_events (
    event_id uuid PRIMARY KEY,
    tenant_id text NOT NULL,
    event_type text NOT NULL,
    session_id text NOT NULL,
    user_id text NOT NULL,
    campaign_id text,
    product_id text,
    quantity integer,
    unit_price numeric(10,2),
    search_term text,
    occurred_at timestamptz NOT NULL,
    received_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS event_streaming_poc.campaign_purchase_revenue(
    tenant_id text not null,
    campaign_id text not null,
    event_id uuid not null,
    user_id text not null,
    revenue_amount numeric(10,2),
    occurred_at timestamptz not null,
    PRIMARY KEY (tenant_id, campaign_id, event_id)
);

CREATE index if not exists ix_raw_events_tenant_campaign on event_streaming_poc.raw_events
(tenant_id, campaign_id, event_type);

CREATE index if not exists ix_campaign_purchase_revenue_lookup on event_streaming_poc.campaign_purchase_revenue
(tenant_id, campaign_id, occurred_at);