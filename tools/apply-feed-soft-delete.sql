ALTER TABLE feed_posts ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone NULL;
ALTER TABLE feed_posts ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;
