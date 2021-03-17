﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Warehouse.Api.Products.Controllers.v1;
using Warehouse.Core.Common;
using Warehouse.Core.DTO.Product;
using Warehouse.Core.Interfaces.Services;

namespace Warehouse.Api.Products.Tests.Controllers
{
    [TestFixture]
    public class ProductsControllerTests
    {
        private ProductDto _product;
        private readonly Mock<IProductService> _productService = new();
        
        private ProductsController _productsController;
        
        [OneTimeSetUp]
        public void SetUpOnce()
        {
            _productsController = new(_productService.Object);
            _product = new("a", "a");
        }
        
        [Test]
        public async Task GetAllAsync_WhenCalled_ReturnsListOfProducts()
        {
            List<ProductDto> products = new(){_product};
            _productService.Setup(ms => ms.GetAllAsync())
                .ReturnsAsync(Result<List<ProductDto>>.Success(products));

            var result = await _productsController.GetAllAsync() as OkObjectResult;

            Assert.That(result?.Value, Is.EqualTo(products));
        }
        
        [Test]
        public async Task GetAsync_WhenCalled_ReturnsProduct()
        {
            _productService.Setup(us => us.GetAsync(_product.Id))
                .ReturnsAsync(Result<ProductDto>.Success(_product));

            var result = await _productsController.GetAsync(_product.Id) as OkObjectResult;

            Assert.That(result?.Value, Is.EqualTo(_product));
        }
        
        [Test]
        public async Task CreateAsync_WhenCalled_ReturnsProduct()
        {
            ProductModelDto product = new("a", DateTime.Now, new List<string>{"a"}, "a");
            _productService.Setup(us => us.CreateAsync(product))
                .ReturnsAsync(Result<ProductDto>.Success(_product));

            var result = await _productsController.CreateAsync(product) as OkObjectResult;

            Assert.That(result?.Value, Is.EqualTo(_product));
        }
        
        [Test]
        public async Task UpdateAsync_WhenCalled_ReturnsProduct()
        {
            ProductModelDto product = new("a", DateTime.Now, new List<string>{"a"}, "a");
            _productService.Setup(us => us.UpdateAsync(_product.Id, product))
                .ReturnsAsync(Result<ProductDto>.Success(_product));

            var result = await _productsController.UpdateAsync(_product.Id, product) as OkObjectResult;

            Assert.That(result?.Value, Is.EqualTo(_product));
        }
        
        [Test]
        public async Task DeleteAsync_WhenCalled_ReturnsProduct()
        {
            _productService.Setup(us => us.DeleteAsync(_product.Id))
                .ReturnsAsync(Result<object>.Success());

            var result = await _productsController.DeleteAsync(_product.Id) as OkObjectResult;

            Assert.That(result?.Value, Is.EqualTo(null));
        }
    }
}