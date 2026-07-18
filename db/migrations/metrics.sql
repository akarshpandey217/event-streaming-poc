create table if not exists retail.session_last_click(
    tenant_id text not null,
    session_id text not null,
    campaign_id text not null,
    clicked_at timestamptz not null,
    primary key (tenant_id, session_id)
);

create table if not exists retail.campaign_event_users(
    tenant_id text not null,
    campaign_id text not null,
    event_type text not null,
    user_id text not null,
    first_seen_at timestamptz not null default now(),
    primary key (tenant_id, campaign_id, event_type, user_id)
);

create index if not exists ix_campaign_event_users_lookup on
retail.campaign_event_users (tenant_id, campaign_id, event_type, first_seen_at);