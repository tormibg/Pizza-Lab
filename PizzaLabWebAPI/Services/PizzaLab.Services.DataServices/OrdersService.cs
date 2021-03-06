﻿namespace PizzaLab.Services.DataServices
{
    using AutoMapper;
    using Contracts;
    using Data.Common;
    using Data.Models;
    using Data.Models.Enums;
    using Microsoft.EntityFrameworkCore;
    using Models.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class OrdersService : IOrdersService
    {
        private readonly IRepository<Order> _ordersRepository;
        private readonly IRepository<OrderProduct> _orderProductRepository;
        private readonly IMapper _mapper;

        public OrdersService(
            IRepository<Order> ordersRepository,
            IRepository<OrderProduct> orderProductRepository,
            IMapper mapper)
        {
            this._ordersRepository = ordersRepository;
            this._orderProductRepository = orderProductRepository;
            this._mapper = mapper;
        }

        public async Task ApproveOrderAsync(string orderId)
        {
            var order = this._ordersRepository
                .All()
                .First(o => o.Id == orderId);

            order.Status = OrderStatus.Approved;
            await this._ordersRepository.SaveChangesAsync();
        }

        public async Task<OrderDto> CreateOrderAsync(string userId, IEnumerable<OrderProductDto> orderProducts)
        {
            var order = new Order
            {
                CreatorId = userId,
                CreationDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                Products = orderProducts
                    .Select(op => this._mapper.Map<OrderProduct>(op))
                    .ToList()
            };

            await this._ordersRepository.AddAsync(order);
            await this._ordersRepository.SaveChangesAsync();

            var createdOrder = this._ordersRepository
                .All()
                .Include(o => o.Products)
                .ThenInclude(p => p.Product)
                .First(o => o.Id == order.Id);

            return this._mapper.Map<OrderDto>(createdOrder);
        }

        public bool Exists(string orderId)
        {
            return this._ordersRepository
                .All()
                .Any(o => o.Id == orderId);
        }

        public IEnumerable<OrderDto> GetApprovedOrders()
        {
            return this._ordersRepository
                .All()
                .Include(o => o.Creator)
                .Include(o => o.Products)
                .ThenInclude(p => p.Product)
                .Where(o => o.Status == OrderStatus.Approved)
                .Select(o => this._mapper.Map<OrderDto>(o));
        }

        public IEnumerable<OrderDto> GetPendingOrders()
        {
            return this._ordersRepository
                .All()
                .Include(o => o.Creator)
                .Include(o => o.Products)
                .ThenInclude(p => p.Product)
                .Where(o => o.Status == OrderStatus.Pending)
                .Select(o => this._mapper.Map<OrderDto>(o));
        }

        public IEnumerable<OrderDto> GetUserOrders(string userId)
        {
            return this._ordersRepository
                .All()
                .Include(o => o.Creator)
                .Include(o => o.Products)
                .ThenInclude(p => p.Product)
                .Where(o => o.CreatorId == userId)
                .Select(o => this._mapper.Map<OrderDto>(o));
        }

        public async Task DeleteProductOrdersAsync(string productId)
        {
            var orderProducts = this._orderProductRepository
                .All()
                .Where(op => op.ProductId == productId)
                .ToList();

            this._orderProductRepository.DeleteRange(orderProducts);

            await this._orderProductRepository.SaveChangesAsync();
        }
    }
}
