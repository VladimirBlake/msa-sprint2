import pkg from 'pg';
const { Pool } = pkg;

export const pool = new Pool({
  host: 'hotelio-db',
  port: 5432,
  user: 'hotelio',
  password: 'hotelio',
  database: 'hotelio'
});