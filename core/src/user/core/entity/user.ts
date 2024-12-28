import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';

export interface UserData {
  id: string;
  email: string;
  password: string;
  createdAt: Date;
  updatedAt: Date;
}

export class User implements Clonable<User> {
  constructor(public user: UserData) {}

  static create = (
    userData: Omit<UserData, 'id' | 'createdAt' | 'updatedAt'>,
  ) => {
    return new User({
      id: generateUlid(),
      createdAt: new Date(),
      updatedAt: new Date(),
      ...userData,
    });
  };

  get data(): UserData {
    return { ...this.user };
  }

  clone = (): User => new User({ ...this.user });
}
