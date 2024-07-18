CREATE TABLE sap_s3_data (
    object_key TEXT,
    s3_bucket TEXT NOT NULL,
    last_modified_utc BIGINT NOT NULL,
    server TEXT NOT NULL,
    client INTEGER NOT NULL,
    business_object TEXT NOT NULL,
    entity_key TEXT NOT NULL,
    event_type TEXT NOT NULL,
    file TEXT NOT NULL,
    event_timestamp BIGINT NOT NULL,
    is_removed BOOLEAN NOT NULL DEFAULT FALSE,
    removed_at_utc BIGINT,
    PRIMARY KEY (object_key)
);


CREATE INDEX idx_event_timestamp ON sap_s3_data(event_timestamp);
CREATE INDEX idx_last_modified_utc ON sap_s3_data(last_modified_utc);

ALTER TABLE sap_s3_data ADD CONSTRAINT unique_object_key UNIQUE (object_key);