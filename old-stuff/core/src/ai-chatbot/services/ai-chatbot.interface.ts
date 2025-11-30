import { ChatCompletionMessageDto } from '../interfaces/dtos/chat-completion.dto';

export abstract class IAiChatbot {
  abstract createChatCompletion(messages: ChatCompletionMessageDto[]): unknown;
}
