SELECT "Key", "Value"
FROM app_settings
WHERE "Key" LIKE 'glpi.%'
   OR "Key" LIKE 'helpdesk.%'
   OR "Key" = 'integrations.use_dev_adapters'
ORDER BY "Key";
