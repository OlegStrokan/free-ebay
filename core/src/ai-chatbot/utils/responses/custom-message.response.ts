export const customMessage = (
  statusCode: number,
  message: string,
  data = {},
): object => {
  return {
    statusCode,
    message: [message],
    data,
  };
};
