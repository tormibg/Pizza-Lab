﻿namespace PizzaLab.WebAPI.Areas.Admin.Controllers
{
    using AutoMapper;
    using Data.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Models.Products.InputModels;
    using Services.DataServices.Contracts;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using WebAPI.Controllers;
    using WebAPI.Models.Common;
    using WebAPI.Models.Products.ViewModels;

    [Route("api/admin/[controller]")]
    public class ProductsController : ApiController
    {
        private readonly IMapper _mapper;
        private readonly IProductsService _productsService;
        private readonly ICategoriesService _categoriesService;
        private readonly IIngredientsService _ingredientsService;
        private readonly IReviewsService _reviewsService;
        private readonly IProductsIngredientsService _productsIngredientsService;
        private readonly IUsersLikesService _usersLikesService;

        public ProductsController(
            IMapper mapper,
            IProductsService productsService,
            ICategoriesService categoriesService,
            IIngredientsService ingredientsService,
            IReviewsService reviewsService,
            IProductsIngredientsService productsIngredientsService,
            IUsersLikesService usersLikesService)
        {
            this._mapper = mapper;
            this._productsService = productsService;
            this._categoriesService = categoriesService;
            this._ingredientsService = ingredientsService;
            this._reviewsService = reviewsService;
            this._productsIngredientsService = productsIngredientsService;
            this._usersLikesService = usersLikesService;
        }

        [HttpPost]
        [ProducesDefaultResponseType]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SuccessViewModel<ProductViewModel>>> Post([FromBody] ProductInputModel model)
        {
            if (this.User.IsInRole("Administrator"))
            {
                if (_productsService.All().Any(p => p.Name == model.Name))
                {
                    return BadRequest(new BadRequestViewModel
                    {
                        Message = "Product with the given name already exists."
                    });
                }

                var productCategory = _categoriesService.FindByName(model.Category);
                if (productCategory != null)
                {
                    var ingredients = new List<Ingredient>();
                    foreach (var ingredientName in model.Ingredients)
                    {
                        var ingredient = this._ingredientsService.FindByName(ingredientName);
                        if (ingredient != null)
                        {
                            ingredients.Add(ingredient);
                        }
                        else
                        {
                            return BadRequest(new BadRequestViewModel
                            {
                                Message = $"{ingredientName} ingredient not found."
                            });
                        }
                    }

                    var product = new Product
                    {
                        Name = model.Name,
                        CategoryId = productCategory.Id,
                        Description = model.Description,
                        Image = model.Image,
                        Weight = model.Weight,
                        Price = model.Price,
                        Ingredients = ingredients.Select(i => new ProductsIngredients
                        {
                            IngredientId = i.Id
                        }).ToList()
                    };

                    try
                    {
                        await this._productsService.CreateAsync(product);

                        return new SuccessViewModel<ProductViewModel>
                        {
                            Data = this._mapper.Map<ProductViewModel>(product),
                            Message = "Product added successfully."
                        };
                    }
                    catch (Exception)
                    {
                        return BadRequest(new BadRequestViewModel
                        {
                            Message = "Something went wrong."
                        });
                    }
                }

                return BadRequest(new BadRequestViewModel
                {
                    Message = "Category not found."
                });
            }

            return Unauthorized();
        }

        [HttpPut("{productId}")]
        [ProducesDefaultResponseType]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SuccessViewModel<ProductViewModel>>> Put([FromRoute] string productId, [FromBody] ProductInputModel model)
        {
            if (this.User.IsInRole("Administrator"))
            {
                if (!_productsService.Exists(productId))
                {
                    return BadRequest(new BadRequestViewModel
                    {
                        Message = "Product with the given id does not exist."
                    });
                }

                var productCategory = _categoriesService.FindByName(model.Category);
                if (productCategory != null)
                {
                    var product = this._productsService
                        .All()
                        .First(p => p.Id == productId);

                    if (product.Name != model.Name && _productsService.All().Any(p => p.Name == model.Name))
                    {
                        return BadRequest(new BadRequestViewModel
                        {
                            Message = "Product with the given name already exists."
                        });
                    }

                    var ingredients = new List<Ingredient>();
                    foreach (var ingredientName in model.Ingredients)
                    {
                        var ingredient = this._ingredientsService.FindByName(ingredientName);
                        if (ingredient != null)
                        {
                            ingredients.Add(ingredient);
                        }
                        else
                        {
                            return BadRequest(new BadRequestViewModel
                            {
                                Message = $"{ingredientName} ingredient not found."
                            });
                        }
                    }

                    await this._productsIngredientsService
                        .DeleteProductIngredientsAsync(productId);

                    product.Name = model.Name;
                    product.CategoryId = productCategory.Id;
                    product.Description = model.Description;
                    product.Image = model.Image;
                    product.Weight = model.Weight;
                    product.Price = model.Price;
                    product.Ingredients = ingredients.Select(i => new ProductsIngredients
                    {
                        IngredientId = i.Id
                    }).ToList();

                    try
                    {
                        await this._productsService.EditAsync(product);

                        return new SuccessViewModel<ProductViewModel>
                        {
                            Data = this._mapper.Map<ProductViewModel>(product),
                            Message = "Product edited successfully."
                        };
                    }
                    catch (Exception)
                    {
                        return BadRequest(new BadRequestViewModel
                        {
                            Message = "Something went wrong."
                        });
                    }
                }

                return BadRequest(new BadRequestViewModel
                {
                    Message = "Category not found."
                });
            }

            return Unauthorized();
        }

        [HttpDelete("{productId}")]
        [ProducesDefaultResponseType]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SuccessViewModel<ProductViewModel>>> Delete(string productId)
        {
            if (this.User.IsInRole("Administrator"))
            {
                if (!_productsService.Exists(productId))
                {
                    return BadRequest(new BadRequestViewModel
                    {
                        Message = "Product with the given id does not exist."
                    });
                }

                try
                {
                    await this._usersLikesService.DeleteProductLikesAsync(productId);
                    await this._productsIngredientsService.DeleteProductIngredientsAsync(productId);
                    await this._reviewsService.DeleteProductReviewsAsync(productId);
                    await this._productsService.DeleteAsync(productId);

                    return Ok(new
                    {
                        Message = "Product deleted successfully."
                    });
                }
                catch (Exception)
                {
                    return BadRequest(new BadRequestViewModel
                    {
                        Message = "Something went wrong."
                    });
                }
            }

            return Unauthorized();
        }
    }
}