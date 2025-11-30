import { User } from './user';
import { UserData } from './user';

describe('User', () => {
  let userData: UserData;
  let user: User;

  beforeEach(() => {
    userData = {
      id: '1',
      email: 'test@example.com',
      password: 'password123',
      createdAt: new Date(),
      updatedAt: new Date(),
    };
    user = new User(userData);
  });

  test('should create a user successfully', () => {
    const newUser = User.create({
      email: 'newuser@example.com',
      password: 'newpassword123',
    });
    expect(newUser).toBeInstanceOf(User);
    expect(newUser.email).toBe('newuser@example.com');
    expect(newUser.password).toBe('newpassword123');
    expect(newUser.createdAt).toBeInstanceOf(Date);
    expect(newUser.updatedAt).toBeInstanceOf(Date);
    expect(newUser.id).toBeDefined();
  });

  test('should update email successfully', () => {
    const updatedUser = user.updateEmail('updated@example.com');
    expect(updatedUser.email).toBe('updated@example.com');
    expect(user.email).toBe('test@example.com');
  });

  test('should update password successfully', () => {
    const updatedUser = user.updatePassword('newpassword456');
    expect(updatedUser.password).toBe('newpassword456');
    expect(user.password).toBe('password123');
  });

  test('should clone user correctly', () => {
    const clonedUser = user.clone();
    expect(clonedUser).toBeInstanceOf(User);
    expect(clonedUser.email).toBe(user.email);
    expect(clonedUser.password).toBe(user.password);
    expect(clonedUser.id).toBe(user.id);
    expect(clonedUser.createdAt).toEqual(user.createdAt);
    expect(clonedUser.updatedAt).toEqual(user.updatedAt);
  });

  test('should retain original user data after email update', () => {
    const updatedUser = user.updateEmail('another@example.com');
    expect(user.email).toBe('test@example.com');
    expect(updatedUser.email).toBe('another@example.com');
  });

  test('should retain original user data after password update', () => {
    const updatedUser = user.updatePassword('newpassword789');
    expect(user.password).toBe('password123');
    expect(updatedUser.password).toBe('newpassword789');
  });

  test('should update using deprecated method', () => {
    const updatedUser = user.update({ email: 'deprecated@example.com' });
    expect(updatedUser.email).toBe('deprecated@example.com');
    expect(user.email).toBe('test@example.com');
  });
});
