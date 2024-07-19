CREATE OR REPLACE PROCEDURE insert_into_test_table(p_name VARCHAR)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO test_table (name) VALUES (p_name);
END;
$$;