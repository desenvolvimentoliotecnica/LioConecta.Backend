UPDATE app_settings SET "Value" = '["http://10.0.0.79:8092","http://localhost:5173"]', "UpdatedAt" = NOW() WHERE "Key" = 'cors.allowed_origins';
UPDATE app_settings SET "Value" = 'redis:6379', "UpdatedAt" = NOW() WHERE "Key" = 'redis.connection';
