export const FernApiEnvironment = {
  Default: 'https://api.example.test',
} as const;

export type FernApiEnvironment = (typeof FernApiEnvironment)[keyof typeof FernApiEnvironment];
