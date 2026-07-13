-- Generated from local dev database for GLPI/helpdesk settings migration.
\set ON_ERROR_STOP on

UPDATE app_settings SET "Value" = 'VAR2gUQQ12ROV6UFcHhgua2efCAy7BPV70K07CqT', "UpdatedAt" = NOW() WHERE "Key" = 'glpi.app_token';
UPDATE app_settings SET "Value" = 'https://servicedesk.liotecnica.com.br/api.php/v1', "UpdatedAt" = NOW() WHERE "Key" = 'glpi.base_url';
UPDATE app_settings SET "Value" = 'https://servicedesk.liotecnica.com.br', "UpdatedAt" = NOW() WHERE "Key" = 'glpi.portal_url';
UPDATE app_settings SET "Value" = '7', "UpdatedAt" = NOW() WHERE "Key" = 'glpi.profile_id';
UPDATE app_settings SET "Value" = 'ocJzsNsUJAFduci9UCeCNP3E9406oryHpFNn50Lp', "UpdatedAt" = NOW() WHERE "Key" = 'glpi.user_token';
DELETE FROM app_settings WHERE "Key" = 'helpdesk.glpi_areas';
UPDATE app_settings SET "Value" = 'false', "UpdatedAt" = NOW() WHERE "Key" = 'integrations.use_dev_adapters';
