SELECT "Key", LEFT("Value", 70) AS preview
FROM app_settings
WHERE "Key" LIKE 'glpi.%'
   OR "Key" LIKE 'helpdesk.%'
   OR "Key" = 'integrations.use_dev_adapters'
   OR "Key" = 'cors.allowed_origins'
ORDER BY "Key";
