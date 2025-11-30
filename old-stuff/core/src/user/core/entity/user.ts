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
  constructor(private user: UserData) {}

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

  get id(): string {
    return this.user.id;
  }

  get email(): string {
    return this.user.email;
  }

  get password(): string {
    return this.user.password;
  }

  get createdAt(): Date {
    return this.user.createdAt;
  }

  get updatedAt(): Date {
    return this.user.updatedAt;
  }

  get data(): UserData {
    return { ...this.user };
  }

  updateEmail = (newEmail: string) => {
    const clone = this.clone();
    clone.user.email = newEmail;
    return clone;
  };

  updatePassword = (newPassword: string) => {
    const clone = this.clone();
    clone.user.password = newPassword;
    return clone;
  };

  // deprecated: will be removed soon
  update(userData: Pick<UserData, 'email'>) {
    return this.updateEmail(userData.email);
  }

  clone = (): User => new User({ ...this.user });
}
