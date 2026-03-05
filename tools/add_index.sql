CREATE INDEX IF NOT EXISTS idx_users_first_last_name ON users (first_name varchar_pattern_ops, last_name varchar_pattern_ops);
