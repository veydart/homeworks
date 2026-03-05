EXPLAIN ANALYZE SELECT id, first_name, last_name, birth_date, gender, interests, city
FROM users
WHERE first_name LIKE 'Ал%' AND last_name LIKE 'Ив%'
ORDER BY id;
