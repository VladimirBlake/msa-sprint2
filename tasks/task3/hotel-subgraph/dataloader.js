import DataLoader from 'dataloader';
import { pool } from './db.js';

export const createHotelLoader = () =>
  new DataLoader(async (ids) => {

    const { rows } = await pool.query(
      `
      SELECT id, city, rating
      FROM hotel
      WHERE id = ANY($1)
      `,
      [ids]
    );

    const hotelMap = new Map(
      rows.map(h => [
        String(h.id),
        {
          id: h.id,
          name: `Hotel ${h.id}`,
          city: h.city,
          stars: Math.round(h.rating)
        }
      ])
    );

    return ids.map(id => hotelMap.get(String(id)) || null);
  });