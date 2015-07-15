Copy (
SELECT 'ALTER TABLE '||nspname||'.'||relname||' DROP CONSTRAINT '||conname||';' as ColumnName
FROM pg_constraint 
INNER JOIN pg_class ON conrelid=pg_class.oid 
INNER JOIN pg_namespace ON pg_namespace.oid=pg_class.relnamespace 
ORDER BY CASE WHEN contype='f' THEN 0 ELSE 1 END,contype,nspname,relname,conname
) To 'C:\Temp\SchemaDB\log\DTL-RemoveConstraints.inp';

