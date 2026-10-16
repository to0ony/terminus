// Odvojeno od queries.ts jer ih koristi i API klijent (client.ts).
export const authKeys = {
  me: ['auth', 'me'] as const,
}
