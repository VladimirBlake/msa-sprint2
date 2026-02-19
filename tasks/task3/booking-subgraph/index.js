import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { buildSubgraphSchema } from '@apollo/subgraph';
import  grpc from '@grpc/grpc-js';
import protoLoader from '@grpc/proto-loader';
import gql from 'graphql-tag';

const PROTO_FILE = 'booking.proto';
const packageDefinition = protoLoader.loadSync(PROTO_FILE, {
  keepCase: true,
  longs: String
});
const protoDescriptor = grpc.loadPackageDefinition(packageDefinition);
const bookingService = protoDescriptor.booking.BookingService;
const stub = new bookingService('booking-service:9090', grpc.credentials.createInsecure());

// stub.ListBookings(bookingListRequest, (error, response) => {
//   if (error) {
//     console.error('Error fetching bookings:', error);
//   } else {
//     console.log('Bookings for user test-user-3:', response.bookings);
//   }
// });

const getListBookingsAsync = (client, request) => {
  return new Promise((resolve, reject) => {
    client.ListBookings(request, (error, response) => {
      if (error) {
        reject(error);
      } else {
        resolve(response);
      }
    });
  });
}

const typeDefs = gql`
  type Booking @key(fields: "id") {
    id: ID!
    userId: String!
    hotel: Hotel
    promoCode: String
    discountPercent: Int
  }

  type Hotel @key(fields: "id") {
    id: ID!
  }

  type Query {
    bookingsByUser(userId: String!): [Booking]
  }

`;

const resolvers = {
  Query: {
    bookingsByUser: async (_, { userId }, { req }) => {
      if (req.headers['userid'] !== userId) {
        throw new Error(`Unauthorized ${req.headers['userid']} trying to access bookings of ${userId}`);
      }
      const bookingRequest = { user_id: userId };
      try {
        const response = await getListBookingsAsync(stub, bookingRequest);
        return response.bookings.map(booking => ({
          id: booking.id,
          userId: booking.user_id,
          hotelId: booking.hotel_id,
          promoCode: booking.promo_code,
          discountPercent: booking.discount_percent ?? 0,
        }));
      }
      catch (error) {
        console.error('Error fetching bookings:', error);
      }
    },
  },
  Booking: {
	  hotel: (booking) => {
      return { id: booking.hotelId };
    }
  },
};

const server = new ApolloServer({
  schema: buildSubgraphSchema([{ typeDefs, resolvers }]),
});

startStandaloneServer(server, {
  listen: { port: 4001 },
  context: async ({ req }) => ({ req }),
}).then(() => {
  console.log('✅ Booking subgraph ready at http://localhost:4001/');
});
