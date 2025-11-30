export type ISO8601 = string;

const assertLocalTZIsUTC = () => {
  if (process.env.TZ !== 'Etc/UTC')
    throw new Error('TZ env var is not set to UTC');
};

export const toISO8601UTC = (date: Date): ISO8601 => {
  assertLocalTZIsUTC();
  return date.toISOString();
};
