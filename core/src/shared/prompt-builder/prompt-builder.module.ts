import { Module } from '@nestjs/common';
import { IPromptBuilderService } from './prompt-builder.interface';
import { PromptBuilderService } from './prompt-builder.service';

@Module({
  providers: [
    {
      provide: IPromptBuilderService,
      useClass: PromptBuilderService,
    },
  ],
  exports: [
    {
      provide: IPromptBuilderService,
      useClass: PromptBuilderService,
    },
  ],
})
export class PromptBuilderModule {}
