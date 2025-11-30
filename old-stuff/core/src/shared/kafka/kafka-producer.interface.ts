export abstract class IKafkaProducerService {
  abstract sendMessage(
    topic: string,
    message: any,
    key?: string,
  ): Promise<void>;
}
