import {
  Controller,
  Get,
  Post,
  Patch,
  Delete,
  Body,
  Param,
  Inject,
} from '@nestjs/common';

import { CreateCategoryDto } from 'src/catalog/interface/dtos/create-category.dto';
import { UpdateCategoryDto } from 'src/catalog/interface/dtos/update-category.dto';
import { ICreateCategoryUseCase } from '../epplication/use-cases/create-category/create-category.interface';
import { IDeleteCategoryUseCase } from '../epplication/use-cases/delete-category/delete-category.interface';
import { IGetAllCategoriesUseCase } from '../epplication/use-cases/get-all-categories/get-all-categories.interface';
import { IGetCategoryByIdUseCase } from '../epplication/use-cases/get-category-by-id/get-category-by-id.interface';
import { IUpdateCategoryUseCase } from '../epplication/use-cases/update-category/update-category.interface';
import {
  CREATE_CATEGORY_USE_CASE,
  DELETE_CATEGORY_USE_CASE,
  GET_ALL_CATEGORIES_USE_CASE,
  GET_CATEGORY_BY_ID_USE_CASE,
  UPDATE_CATEGORY_USE_CASE,
} from '../epplication/injection-tokens/use-case.token';

@Controller('catalog')
export class CatalogController {
  constructor(
    @Inject(GET_ALL_CATEGORIES_USE_CASE)
    private readonly getAllCategoriesUseCase: IGetAllCategoriesUseCase,
    @Inject(GET_CATEGORY_BY_ID_USE_CASE)
    private readonly getCategoryByIdUseCase: IGetCategoryByIdUseCase,
    @Inject(CREATE_CATEGORY_USE_CASE)
    private readonly createCategoryUseCase: ICreateCategoryUseCase,
    @Inject(UPDATE_CATEGORY_USE_CASE)
    private readonly updateCategoryUseCase: IUpdateCategoryUseCase,
    @Inject(DELETE_CATEGORY_USE_CASE)
    private readonly deleteCategoryUseCase: IDeleteCategoryUseCase,
  ) {}

  @Get('categories')
  async getAllCategories() {
    return this.getAllCategoriesUseCase.execute();
  }

  @Get('category/:id')
  async getCategory(@Param('id') id: string) {
    return this.getCategoryByIdUseCase.execute(id);
  }

  @Post('categories')
  async createCategory(@Body() createCategoryDto: CreateCategoryDto) {
    return this.createCategoryUseCase.execute(createCategoryDto);
  }

  @Patch('category/:id')
  async updateCategory(
    @Param('id') id: string,
    @Body() updateCategoryDto: UpdateCategoryDto,
  ) {
    return this.updateCategoryUseCase.execute({ id, dto: updateCategoryDto });
  }

  @Delete('category/:id')
  async deleteCategory(@Param('id') id: string) {
    return this.deleteCategoryUseCase.execute(id);
  }
}
