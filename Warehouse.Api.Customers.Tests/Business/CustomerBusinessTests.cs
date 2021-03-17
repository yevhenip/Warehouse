﻿using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Warehouse.Api.Customers.Business;
using Warehouse.Core.Common;
using Warehouse.Core.DTO.Customer;
using Warehouse.Core.Interfaces.Messaging.Sender;
using Warehouse.Core.Interfaces.Repositories;
using Warehouse.Core.Interfaces.Services;
using Warehouse.Core.Settings.CacheSettings;
using Warehouse.Domain;

namespace Warehouse.Api.Customers.Tests.Business
{
    [TestFixture]
    public class CustomerBusinessTests
    {
        private CustomerService _customerService;

        private readonly CustomerDto _customer = new("a", "a", "a", "a");
        private readonly Customer _dbCustomer = new() {Email = "a", Id = "a", FullName = "a", Phone = "a"};
        private readonly Mock<ICustomerRepository> _customerRepository = new();
        private readonly Mock<IOptions<CacheCustomerSettings>> _options = new();
        private readonly Mock<IFileService> _fileService = new();
        private readonly Mock<IDistributedCache> _cache = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ISender> _sender = new();

        [OneTimeSetUp]
        public void SetUpOnce()
        {
            _options.Setup(opt => opt.Value).Returns(new CacheCustomerSettings
                {AbsoluteExpiration = 1, SlidingExpiration = 1});
            _customerService = new(_options.Object, _customerRepository.Object, _cache.Object, _mapper.Object,
                _sender.Object, _fileService.Object);
        }

        [Test]
        public async Task GetAllAsync_WhenCalled_ReturnsListOfCustomers()
        {
            var customers = ConfigureGetAll();

            var result = await _customerService.GetAllAsync();

            Assert.That(result.Data, Is.EqualTo(customers));
        }

        [Test]
        public async Task GetAsync_WhenCalledWithUnCashedCustomer_ReturnsCustomer()
        {
            ConfigureGet(_dbCustomer, _dbCustomer);

            var result = await _customerService.GetAsync(_dbCustomer.Id);

            Assert.That(result.Data, Is.EqualTo(_customer));
        }

        [Test]
        public async Task GetAsync_WhenCalledWithFiledCustomer_ReturnsCustomer()
        {
            ConfigureGet(null, _dbCustomer);

            var result = await _customerService.GetAsync(_dbCustomer.Id);

            Assert.That(result.Data, Is.EqualTo(_customer));
        }

        [Test]
        public void GetAsync_WhenCalledWithUndefinedCustomer_ThrowsResultOfCustomerException()
        {
            ConfigureGet(null, null);

            Assert.ThrowsAsync<Result<Customer>>(
                async () => await _customerService.GetAsync(_dbCustomer.Id));
        }

        [Test]
        public async Task CreateAsync_WhenCalledWithUniqueData_ReturnsCustomer()
        {
            ConfigureCreate();

            var result = await _customerService.CreateAsync(_customer);

            Assert.That(result.Data, Is.EqualTo(_customer));
        }

        [Test]
        public void DeleteAsync_WhenCalledWithUnCachedCustomerAndUnExistingId_ThrowsResultOfCustomerException()
        {
            ConfigureDelete(null);

            Assert.ThrowsAsync<Result<Customer>>(async () =>
                await _customerService.DeleteAsync(_dbCustomer.Id));
        }

        [Test]
        public async Task DeleteAsync_WhenCalledWithUnCachedCustomerAndExistingId_ReturnsNull()
        {
            ConfigureDelete(_dbCustomer);
            var result = await _customerService.DeleteAsync(_dbCustomer.Id);

            Assert.That(result.Data, Is.EqualTo(null));
        }

        private List<CustomerDto> ConfigureGetAll()
        {
            List<CustomerDto> customers = new() {_customer};
            List<Customer> customersFromDb = new() {_dbCustomer};
            _customerRepository.Setup(ur => ur.GetAllAsync()).ReturnsAsync(customersFromDb);
            _mapper.Setup(m => m.Map<List<CustomerDto>>(customersFromDb)).Returns(customers);
            return customers;
        }

        private void ConfigureGet(Customer dbCustomer, Customer fileCustomer)
        {
            _customerRepository.Setup(ur => ur.GetAsync(_dbCustomer.Id)).ReturnsAsync(dbCustomer);
            _fileService.Setup(ur => ur.ReadFromFileAsync<Customer>(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(fileCustomer);
            _mapper.Setup(m => m.Map<CustomerDto>(_dbCustomer)).Returns(_customer);
        }

        private void ConfigureCreate()
        {
            _mapper.Setup(m => m.Map<CustomerDto>(_customer)).Returns(_customer);
            _mapper.Setup(m => m.Map<Customer>(_customer)).Returns(_dbCustomer);
        }

        private void ConfigureDelete(Customer customer)
        {
            _customerRepository.Setup(ur => ur.GetAsync(_dbCustomer.Id)).ReturnsAsync(customer);
        }
    }
}