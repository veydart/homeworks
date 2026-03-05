-- Распределение таблицы dialog_messages по dialog_key через Citus
-- Выполнить ПОСЛЕ EF миграции на координаторе
SELECT create_distributed_table('DialogMessages', 'DialogKey');
