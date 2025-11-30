import { Product } from 'src/product/core/product/entity/product';
import { generateUlid } from 'src/shared/types/generate-ulid';

export interface CategoryData {
  id: string;
  name: string;
  description: string;
  parentCategoryId?: string;
  children: Category[];
  products: Product[];
}

export class Category {
  constructor(private category: CategoryData) {}

  static create = (
    categoryData: Omit<CategoryData, 'id' | 'children'>,
  ): Category => {
    return new Category({
      ...categoryData,
      id: generateUlid(),
      products: categoryData.products ?? [],
      children: [],
    });
  };

  get id(): string {
    return this.category.id;
  }

  get data(): CategoryData {
    return this.category;
  }

  get name(): string {
    return this.category.name;
  }

  get description(): string {
    return this.category.description;
  }

  get parentCategoryId(): string | undefined {
    return this.category.parentCategoryId;
  }

  get children(): Category[] {
    return this.category.children;
  }

  get products(): Product[] {
    return this.category.products;
  }

  addChild(child: Category): void {
    this.category.children.push(child);
  }

  addProduct(product: Product): void {
    this.category.products.push(product);
  }

  changeName(newName: string): void {
    this.category.name = newName;
  }

  changeDescription(newDescription: string): void {
    this.category.description = newDescription;
  }
}
