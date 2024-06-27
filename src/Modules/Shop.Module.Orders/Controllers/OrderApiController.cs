﻿using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Infrastructure;
using Shop.Infrastructure.Data;
using Shop.Infrastructure.Web.StandardTable;
using Shop.Module.Catalog.Entities;
using Shop.Module.Catalog.Models;
using Shop.Module.Core.Entities;
using Shop.Module.Core.Extensions;
using Shop.Module.Core.Models;
using Shop.Module.Core.Services;
using Shop.Module.Inventory.Entities;
using Shop.Module.Orders.Data;
using Shop.Module.Orders.Entities;
using Shop.Module.Orders.Events;
using Shop.Module.Orders.Models;
using Shop.Module.Orders.Services;
using Shop.Module.Orders.ViewModels;
using Shop.Module.Shipments.Entities;

namespace Shop.Module.Orders.Controllers;

/// <summary>
/// Admin Order API Controller, handles order management and operations.
/// </summary>
[Authorize(Roles = "admin")]
[Route("api/orders")]
public class OrderApiController : ControllerBase
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<UserAddress> _userAddressRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<OrderAddress> _orderAddressRepository;
    private readonly IWorkContext _workContext;
    private readonly ICountryService _countryService;
    private readonly IMediator _mediator;
    private readonly IRepository<Stock> _stockRepository;
    private readonly IRepository<StockHistory> _stockHistoryRepository;
    private readonly IRepository<Shipment> _shipmentRepository;
    private readonly IOrderService _orderService;
    private readonly IAppSettingService _appSettingService;

    public OrderApiController(
        IRepository<Order> orderRepository,
        IRepository<User> userRepository,
        IRepository<UserAddress> userAddressRepository,
        IRepository<Product> productRepository,
        IRepository<OrderAddress> orderAddressRepository,
        IWorkContext workContext,
        ICountryService countryService,
        IMediator mediator,
        IRepository<Stock> stockRepository,
        IRepository<StockHistory> stockHistoryRepository,
        IRepository<Shipment> shipmentRepository,
        IOrderService orderService,
        IAppSettingService appSettingService)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _userAddressRepository = userAddressRepository;
        _productRepository = productRepository;
        _orderAddressRepository = orderAddressRepository;
        _workContext = workContext;
        _countryService = countryService;
        _mediator = mediator;
        _stockRepository = stockRepository;
        _stockHistoryRepository = stockHistoryRepository;
        _shipmentRepository = shipmentRepository;
        _orderService = orderService;
        _appSettingService = appSettingService;
    }

    /// <summary>
    /// Get order details based on order ID.
    /// </summary>
    /// <param name="id">Order ID. </param>
    /// <returns>Order details. </returns>
    [HttpGet("{id:int:min(1)}")]
    public async Task<Result<OrderGetResult>> Get(int id)
    {
        var result = await GetOrder(id);
        return Result.Ok(result);
    }

    /// <summary>
    /// Get order details based on the order number.
    /// </summary>
    /// <param name="no">Order number. </param>
    /// <returns>Order details. </returns>
    [HttpGet("{no:long:min(1)}/no")]
    public async Task<Result<OrderGetResult>> GetByNo(long no)
    {
        var result = await GetOrder(0, no);
        return Result.Ok(result);
    }

    /// <summary>
    /// Create a new order.
    /// </summary>
    /// <param name="model">Order creation parameters. </param>
    /// <returns>The result of the creation operation. </returns>
    [HttpPost]
    public async Task<Result> Post([FromBody] OrderCreateParam model)
    {
        if (model == null)
            throw new Exception("Parameter abnormality");
        if (model.Items == null || model.Items.Count <= 0)
            throw new Exception("Please add a product");
        if (model.Items.Any(c => c.Quantity <= 0))
            throw new Exception("Quantity of goods to be purchased > 0");

        var user = await _workContext.GetCurrentUserAsync();

        var customer = await _userRepository.FirstOrDefaultAsync(model.CustomerId);
        if (customer == null)
            throw new Exception("Customer does not exist");

        var order = new Order()
        {
            OrderStatus = OrderStatus.New,
            CreatedBy = user,
            UpdatedBy = user,
            CustomerId = model.CustomerId,
            AdminNote = model.AdminNote,
            OrderNote = model.OrderNote,
            ShippingMethod = model.ShippingMethod,
            PaymentType = model.PaymentType,
            ShippingFeeAmount = model.ShippingFeeAmount,
            OrderTotal = model.OrderTotal,
            DiscountAmount = model.DiscountAmount
        };

        OrderAddress orderShipping = null;
        OrderAddress orderBilling = null;
        if (model.ShippingUserAddressId.HasValue && model.ShippingUserAddressId.Value > 0)
            orderShipping = await UserAddressToOrderAddress(model.ShippingUserAddressId.Value, customer.Id,
                AddressType.Shipping, order);
        if (model.BillingUserAddressId.HasValue && model.BillingUserAddressId.Value > 0)
            orderBilling = await UserAddressToOrderAddress(model.BillingUserAddressId.Value, customer.Id,
                AddressType.Billing, order);

        if (model.ShippingAddress != null && orderShipping == null)
            orderShipping = new OrderAddress()
            {
                Order = order,
                AddressType = AddressType.Shipping,
                AddressLine1 = model.ShippingAddress.AddressLine1,
                AddressLine2 = model.ShippingAddress.AddressLine2,
                City = model.ShippingAddress.City,
                Company = model.ShippingAddress.Company,
                ContactName = model.ShippingAddress.ContactName,
                CountryId = model.ShippingAddress.CountryId,
                Email = model.ShippingAddress.Email,
                Phone = model.ShippingAddress.Phone,
                StateOrProvinceId = model.ShippingAddress.StateOrProvinceId,
                ZipCode = model.ShippingAddress.ZipCode
            };
        if (model.BillingAddress != null && orderBilling == null)
            orderBilling = new OrderAddress()
            {
                Order = order,
                AddressType = AddressType.Billing,
                AddressLine1 = model.BillingAddress.AddressLine1,
                AddressLine2 = model.BillingAddress.AddressLine2,
                City = model.BillingAddress.City,
                Company = model.BillingAddress.Company,
                ContactName = model.BillingAddress.ContactName,
                CountryId = model.BillingAddress.CountryId,
                Email = model.BillingAddress.Email,
                Phone = model.BillingAddress.Phone,
                StateOrProvinceId = model.BillingAddress.StateOrProvinceId,
                ZipCode = model.BillingAddress.ZipCode
            };

        var productIds = model.Items.Select(c => c.Id).Distinct();
        var products = await _productRepository.Query()
            .Include(c => c.ThumbnailImage)
            .Where(c => productIds.Contains(c.Id)).ToListAsync();

        if (productIds.Count() <= 0)
            throw new Exception("Product does not exist");

        var stocks = await _stockRepository.Query().Where(c => productIds.Contains(c.ProductId)).ToListAsync();
        var addStockHistories = new List<StockHistory>();
        foreach (var item in products)
        {
            var first = model.Items.FirstOrDefault(c => c.Id == item.Id);
            if (first == null)
                throw new Exception($"product[{item.Name}]does not exist");

            if (!item.IsPublished)
                throw new Exception($"product[{item.Name}]Unpublished");
            if (!item.IsAllowToOrder)
                throw new Exception($"product[{item.Name}]No purchase allowed");

            OrderStockDoWorker(stocks, addStockHistories, item, user, -first.Quantity, order, "Create Order");

            var orderItem = new OrderItem()
            {
                Order = order,
                Product = item,
                ItemWeight = 0,
                ItemAmount = first.Quantity * first.ProductPrice - first.DiscountAmount,
                Quantity = first.Quantity,
                ProductPrice = first.ProductPrice,
                DiscountAmount = first.DiscountAmount,
                CreatedBy = user,
                UpdatedBy = user,
                ProductName = item.Name,
                ProductMediaUrl = item.ThumbnailImage?.Url
            };
            order.OrderItems.Add(orderItem);
        }

        order.SubTotal = order.OrderItems.Sum(c => c.Quantity * c.ProductPrice);
        order.SubTotalWithDiscount = order.OrderItems.Sum(c => c.DiscountAmount);
        _orderRepository.Add(order);

        // Unable to save changes because a circular dependency was detected in the data to be saved
        // https://github.com/aspnet/EntityFrameworkCore/issues/11888
        // https://docs.microsoft.com/zh-cn/ef/core/saving/transactions
        // https://stackoverflow.com/questions/40073149/entity-framework-circular-dependency-for-last-entity
        using (var transaction = _orderRepository.BeginTransaction())
        {
            await _orderRepository.SaveChangesAsync();

            order.ShippingAddress = orderShipping;
            order.BillingAddress = orderBilling;
            await _orderRepository.SaveChangesAsync();

            var orderCreated = new OrderCreated
            {
                OrderId = order.Id,
                Order = order,
                UserId = order.CreatedById,
                Note = order.OrderNote
            };
            await _mediator.Publish(orderCreated);

            await _stockRepository.SaveChangesAsync();
            if (addStockHistories.Count > 0)
            {
                _stockHistoryRepository.AddRange(addStockHistories);
                await _stockHistoryRepository.SaveChangesAsync();
            }

            transaction.Commit();
        }

        //TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }
        //using (var ts = new TransactionScope())
        //{
        //    ts.Complete();
        //}
        return Result.Ok();
    }

    /// <summary>
    /// Update order information.
    /// </summary>
    /// <param name="id">Order ID. </param>
    /// <param name="model">Order editing parameters. </param>
    /// <returns>The result of the update operation. </returns>
    [HttpPut("{id:int:min(1)}")]
    public async Task<Result> Put(int id, [FromBody] OrderEditParam model)
    {
        if (model == null)
            throw new Exception("Parameter abnormality");
        if (model.Items == null || model.Items.Count <= 0)
            throw new Exception("Please add a product");
        if (model.Items.Any(c => c.Quantity <= 0))
            throw new Exception("Quantity of goods to be purchased > 0");

        var currentUser = await _workContext.GetCurrentUserAsync();
        var order = await _orderRepository
            .Query()
            .Include(c => c.Customer)
            .Include(c => c.BillingAddress)
            .Include(c => c.ShippingAddress)
            .Include(c => c.OrderItems)
            .Where(c => c.Id == id).FirstOrDefaultAsync();
        if (order == null)
            throw new Exception("The order does not exist");

        var user = await _workContext.GetCurrentUserAsync();
        var oldStatus = order.OrderStatus;

        order.ShippingAddressId = model.ShippingAddressId;
        order.BillingAddressId = model.BillingAddressId;

        if (model.ShippingAddress != null)
        {
            if (order.ShippingAddress == null)
            {
                order.ShippingAddress = new OrderAddress()
                {
                    Order = order,
                    AddressType = AddressType.Shipping,
                    AddressLine1 = model.ShippingAddress.AddressLine1,
                    AddressLine2 = model.ShippingAddress.AddressLine2,
                    City = model.ShippingAddress.City,
                    Company = model.ShippingAddress.Company,
                    ContactName = model.ShippingAddress.ContactName,
                    CountryId = model.ShippingAddress.CountryId,
                    Email = model.ShippingAddress.Email,
                    Phone = model.ShippingAddress.Phone,
                    StateOrProvinceId = model.ShippingAddress.StateOrProvinceId,
                    ZipCode = model.ShippingAddress.ZipCode
                };
            }
            else
            {
                order.ShippingAddress.AddressType = AddressType.Shipping;
                order.ShippingAddress.AddressLine1 = model.ShippingAddress.AddressLine1;
                order.ShippingAddress.AddressLine2 = model.ShippingAddress.AddressLine2;
                order.ShippingAddress.City = model.ShippingAddress.City;
                order.ShippingAddress.Company = model.ShippingAddress.Company;
                order.ShippingAddress.ContactName = model.ShippingAddress.ContactName;
                order.ShippingAddress.CountryId = model.ShippingAddress.CountryId;
                order.ShippingAddress.Email = model.ShippingAddress.Email;
                order.ShippingAddress.Phone = model.ShippingAddress.Phone;
                order.ShippingAddress.StateOrProvinceId = model.ShippingAddress.StateOrProvinceId;
                order.ShippingAddress.ZipCode = model.ShippingAddress.ZipCode;
            }
        }

        if (model.BillingAddress != null)
        {
            if (order.BillingAddress == null)
            {
                order.BillingAddress = new OrderAddress()
                {
                    Order = order,
                    AddressType = AddressType.Shipping,
                    AddressLine1 = model.BillingAddress.AddressLine1,
                    AddressLine2 = model.BillingAddress.AddressLine2,
                    City = model.BillingAddress.City,
                    Company = model.BillingAddress.Company,
                    ContactName = model.BillingAddress.ContactName,
                    CountryId = model.BillingAddress.CountryId,
                    Email = model.BillingAddress.Email,
                    Phone = model.BillingAddress.Phone,
                    StateOrProvinceId = model.BillingAddress.StateOrProvinceId,
                    ZipCode = model.BillingAddress.ZipCode
                };
            }
            else
            {
                order.BillingAddress.AddressType = AddressType.Shipping;
                order.BillingAddress.AddressLine1 = model.BillingAddress.AddressLine1;
                order.BillingAddress.AddressLine2 = model.BillingAddress.AddressLine2;
                order.BillingAddress.City = model.BillingAddress.City;
                order.BillingAddress.Company = model.BillingAddress.Company;
                order.BillingAddress.ContactName = model.BillingAddress.ContactName;
                order.BillingAddress.CountryId = model.BillingAddress.CountryId;
                order.BillingAddress.Email = model.BillingAddress.Email;
                order.BillingAddress.Phone = model.BillingAddress.Phone;
                order.BillingAddress.StateOrProvinceId = model.BillingAddress.StateOrProvinceId;
                order.BillingAddress.ZipCode = model.BillingAddress.ZipCode;
            }
        }

        var productIds = model.Items.Select(c => c.Id).Distinct();
        var products = await _productRepository.Query()
            .Include(c => c.ThumbnailImage)
            .Where(c => productIds.Contains(c.Id)).ToListAsync();
        if (productIds.Count() <= 0)
            throw new Exception("Product does not exist");

        var stocks = await _stockRepository.Query().Where(c => productIds.Contains(c.ProductId)).ToListAsync();
        var addStockHistories = new List<StockHistory>();
        foreach (var item in products)
        {
            var first = model.Items.FirstOrDefault(c => c.Id == item.Id);
            if (first == null)
                throw new Exception($"Product[{item.Name}]does not exist");

            if (!item.IsPublished)
                throw new Exception($"Product[{item.Name}]Unpublished");
            if (!item.IsAllowToOrder)
                throw new Exception($"Product[{item.Name}]No purchase allowed");

            var productStocks = stocks.Where(c => c.ProductId == item.Id);

            if (order.OrderItems.Any(c => c.ProductId == item.Id))
            {
                var orderItem = order.OrderItems.First(c => c.ProductId == item.Id);

                OrderStockDoWorker(stocks, addStockHistories, item, user, orderItem.Quantity - first.Quantity, order,
                    "Modify the quantity of ordered goods");

                orderItem.ItemWeight = 0;
                orderItem.ItemAmount = first.Quantity * first.ProductPrice - first.DiscountAmount;
                orderItem.Quantity = first.Quantity;
                orderItem.ProductPrice = first.ProductPrice;
                orderItem.DiscountAmount = first.DiscountAmount;
                orderItem.UpdatedBy = user;
                orderItem.ProductName = item.Name;
                orderItem.ProductMediaUrl = item.ThumbnailImage?.Url;
                orderItem.UpdatedOn = DateTime.Now;
            }
            else
            {
                OrderStockDoWorker(stocks, addStockHistories, item, user, -first.Quantity, order, "Modify order and add products");

                var orderItem = new OrderItem()
                {
                    Order = order,
                    Product = item,
                    ItemWeight = 0,
                    ItemAmount = first.Quantity * first.ProductPrice - first.DiscountAmount,
                    Quantity = first.Quantity,
                    ProductPrice = first.ProductPrice,
                    DiscountAmount = first.DiscountAmount,
                    CreatedBy = user,
                    UpdatedBy = user,
                    ProductName = item.Name,
                    ProductMediaUrl = item.ThumbnailImage?.Url
                };
                order.OrderItems.Add(orderItem);
            }
        }

        var deletedProductItems = order.OrderItems.Where(c => !productIds.Contains(c.ProductId));
        foreach (var item in deletedProductItems)
        {
            item.IsDeleted = true;
            item.UpdatedOn = DateTime.Now;
        }

        order.OrderStatus = model.OrderStatus;
        order.DiscountAmount = model.DiscountAmount;
        order.OrderTotal = model.OrderTotal;
        order.OrderNote = model.OrderNote;
        order.AdminNote = model.AdminNote;
        order.PaymentType = model.PaymentType;
        order.ShippingFeeAmount = model.ShippingFeeAmount;
        order.ShippingMethod = model.ShippingMethod;
        order.ShippingStatus = model.ShippingStatus;
        order.PaymentMethod = model.PaymentMethod;
        order.PaymentFeeAmount = model.PaymentFeeAmount;
        order.PaymentOn = model.PaymentOn;
        order.ShippedOn = model.ShippedOn;
        order.DeliveredOn = model.DeliveredOn;
        order.CancelOn = model.CancelOn;
        order.CancelReason = model.CancelReason;
        order.RefundAmount = model.RefundAmount;
        order.RefundOn = model.RefundOn;
        order.RefundReason = model.RefundReason;
        order.RefundStatus = model.RefundStatus;

        order.SubTotal = order.OrderItems.Sum(c => c.Quantity * c.ProductPrice);
        order.SubTotalWithDiscount = order.OrderItems.Sum(c => c.DiscountAmount);

        using (var transaction = _orderRepository.BeginTransaction())
        {
            await _orderRepository.SaveChangesAsync();

            var orderStatusChanged = new OrderChanged
            {
                OrderId = order.Id,
                OldStatus = oldStatus,
                NewStatus = order.OrderStatus,
                Order = order,
                UserId = currentUser.Id,
                Note = model.AdminNote
            };
            await _mediator.Publish(orderStatusChanged);

            await _stockRepository.SaveChangesAsync();
            if (addStockHistories.Count > 0)
            {
                _stockHistoryRepository.AddRange(addStockHistories);
                await _stockHistoryRepository.SaveChangesAsync();
            }

            transaction.Commit();
        }

        return Result.Ok();
    }

    /// <summary>
    /// Delete the specified order.
    /// </summary>
    /// <param name="id">Order ID. </param>
    /// <returns>The result of the delete operation. </returns>
    [HttpDelete("{id:int:min(1)}")]
    public async Task<Result> Delete(int id)
    {
        var currentUser = await _workContext.GetCurrentUserAsync();
        var order = await _orderRepository
            .Query()
            .Include(c => c.BillingAddress)
            .Include(c => c.ShippingAddress)
            .Include(c => c.OrderItems).ThenInclude(c => c.Product)
            .Where(c => c.Id == id).FirstOrDefaultAsync();
        if (order == null) return Result.Fail("The order does not exist");
        var orderSs = new OrderStatus[] { OrderStatus.Complete, OrderStatus.Canceled };
        if (!orderSs.Contains(order.OrderStatus)) return Result.Fail("The current order status does not allow deletion");

        if (order.ShippingAddress != null)
        {
            order.ShippingAddress.IsDeleted = true;
            order.ShippingAddress.UpdatedOn = DateTime.Now;
        }

        if (order.BillingAddress != null)
        {
            order.BillingAddress.IsDeleted = true;
            order.BillingAddress.UpdatedOn = DateTime.Now;
        }

        foreach (var item in order.OrderItems)
        {
            item.IsDeleted = true;
            item.UpdatedOn = DateTime.Now;
            item.UpdatedBy = currentUser;
        }

        //Deleting an order does not delete the history.

        order.IsDeleted = true;
        order.UpdatedOn = DateTime.Now;
        order.UpdatedBy = currentUser;

        await _orderRepository.SaveChangesAsync();
        return Result.Ok();
    }

    /// <summary>
    /// Cancel the specified order.
    /// </summary>
    /// <param name="id">Order ID. </param>
    /// <param name="reason">Reason for canceling the order. </param>
    /// <returns>Result of the cancel operation. </returns>
    [HttpPut("{id:int:min(1)}/cancel")]
    public async Task<Result> Cancel(int id, [FromBody] OrderCancelParam reason)
    {
        var currentUser = await _workContext.GetCurrentUserAsync();
        await _orderService.Cancel(id, currentUser.Id, reason?.Reason);
        return Result.Ok();
    }

    /// <summary>
    /// Suspend the specified order.
    /// </summary>
    /// <param name="id">Order ID. </param>
    /// <param name="param">Parameters of the suspended order. </param>
    /// <returns>Results of the suspension operation. </returns>
    [HttpPut("{id:int:min(1)}/on-hold")]
    public async Task<Result> OnHold(int id, [FromBody] OrderOnHoldParam param)
    {
        var currentUser = await _workContext.GetCurrentUserAsync();
        var order = await _orderRepository
            .Query()
            .Where(c => c.Id == id).FirstOrDefaultAsync();
        if (order == null)
            return Result.Fail("The order does not exist");

        if (order.OrderStatus == OrderStatus.OnHold)
            return Result.Fail("Order is suspended");

        var orderNotSs = new OrderStatus[] { OrderStatus.Canceled, OrderStatus.Complete };
        if (orderNotSs.Contains(order.OrderStatus))
            return Result.Fail("The current order status does not allow suspension");

        var oldStatus = order.OrderStatus;

        order.OrderStatus = OrderStatus.OnHold;
        order.OnHoldReason = param?.Reason;
        order.UpdatedOn = DateTime.Now;
        order.UpdatedBy = currentUser;
        await _orderRepository.SaveChangesAsync();

        var orderStatusChanged = new OrderChanged
        {
            OrderId = order.Id,
            OldStatus = oldStatus,
            NewStatus = order.OrderStatus,
            Order = order,
            UserId = currentUser.Id,
            Note = "Pending Orders"
        };
        await _mediator.Publish(orderStatusChanged);

        return Result.Ok();
    }

    /// <summary>
    /// Marks the specified order as paid.
    /// </summary>
    /// <param name="id">Order ID. </param>
    /// <returns>Marks the result of the operation. </returns>
    [HttpPut("{id:int:min(1)}/payment")]
    public async Task<Result> AdminPayment(int id)
    {
        var currentUser = await _workContext.GetCurrentOrThrowAsync();
        var order = await _orderRepository.Query().Where(c => c.Id == id).FirstOrDefaultAsync();
        if (order == null)
            return Result.Fail("The order does not exist");

        var orderSs = new OrderStatus[] { OrderStatus.New, OrderStatus.PendingPayment, OrderStatus.PaymentFailed };
        if (!orderSs.Contains(order.OrderStatus)) return Result.Fail("The current order status does not allow marking for payment");

        await _orderService.PaymentReceived(new PaymentReceivedParam()
        {
            OrderId = order.Id,
            Note = "Mark Payment"
        });

        return Result.Ok();
    }

    /// <summary>
    /// Get the order list by page.
    /// </summary>
    /// <param name="param">Page query parameters. </param>
    /// <returns>Paged order list. </returns>
    [HttpPost("grid")]
    public async Task<Result<StandardTableResult<OrderQueryResult>>> List(
        [FromBody] StandardTableParam<OrderQueryParam> param)
    {
        var query = _orderRepository.Query();
        var search = param.Search;
        if (search != null)
        {
            if (search.CustomerId.HasValue)
                query = query.Where(c => c.CustomerId == search.CustomerId.Value);
            if (search.CreatedOnStart.HasValue)
                query = query.Where(c => c.CreatedOn >= search.CreatedOnStart.Value);
            if (search.CreatedOnEnd.HasValue)
                query = query.Where(c => c.CreatedOn < search.CreatedOnEnd.Value.AddDays(1));
            if (search.OrderStatus.Count > 0)
                query = query.Where(c => search.OrderStatus.Contains(c.OrderStatus));
            if (search.ShippingStatus.Count > 0)
                query = query.Where(c => search.ShippingStatus.Contains(c.ShippingStatus.Value));

            if (!string.IsNullOrWhiteSpace(search.ProductName))
                query = query.Where(c => c.OrderItems.Any(x => x.Product.Name.Contains(search.ProductName)));
            if (!string.IsNullOrWhiteSpace(search.Sku))
                query = query.Where(c => c.OrderItems.Any(x => x.Product.Sku.Contains(search.Sku)));
        }

        var result = await query.Include(c => c.Customer)
            .ToStandardTableResult(param, c => new OrderQueryResult
            {
                Id = c.Id,
                No = c.No.ToString(),
                AdminNote = c.AdminNote,
                BillingAddressId = c.BillingAddressId,
                CancelOn = c.CancelOn,
                CancelReason = c.CancelReason,
                CreatedById = c.CreatedById,
                CreatedOn = c.CreatedOn,
                CustomerId = c.CustomerId,
                CustomerName = c.Customer.FullName,
                DiscountAmount = c.DiscountAmount,
                OrderNote = c.OrderNote,
                OrderStatus = c.OrderStatus,
                OrderTotal = c.OrderTotal,
                PaymentFeeAmount = c.PaymentFeeAmount,
                PaymentMethod = c.PaymentMethod,
                PaymentOn = c.PaymentOn,
                PaymentType = c.PaymentType,
                ShippingAddressId = c.ShippingAddressId,
                ShippingFeeAmount = c.ShippingFeeAmount,
                ShippingMethod = c.ShippingMethod,
                ShippingStatus = c.ShippingStatus,
                UpdatedById = c.UpdatedById,
                UpdatedOn = c.UpdatedOn
            });
        return Result.Ok(result);
    }

    /// <summary>
    /// Create a shipping record for the specified order.
    /// </summary>
    /// <param name="id">Order ID. </param>
    /// <param name="param">Order shipping parameters. </param>
    /// <returns>The result of creating a shipping record. </returns>
    [HttpPost("{id:int:min(1)}/shipment")]
    public async Task<Result> Post(int id, [FromBody] OrderShipmentParam param)
    {
        var currentUser = await _workContext.GetCurrentUserAsync();
        var order = await _orderRepository.Query()
            .Include(c => c.OrderItems).ThenInclude(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (order == null)
            return Result.Fail("The order does not exist");
        if (order.OrderStatus != OrderStatus.Shipping && order.OrderStatus != OrderStatus.PaymentReceived)
            return Result.Fail($"The order status is[{order.OrderStatus}]，No shipment allowed");

        switch (order.ShippingStatus)
        {
            case ShippingStatus.NoShipping:
            {
                //No logistics
                order.OrderStatus = OrderStatus.Shipped;
                await _orderRepository.SaveChangesAsync();
                return Result.Ok();
            }
            case null:
            case ShippingStatus.NotYetShipped:
            case ShippingStatus.PartiallyShipped:
                order.ShippingStatus = ShippingStatus.PartiallyShipped;
                break;

            case ShippingStatus.Shipped:
                return Result.Fail($"Order shipped");

            case ShippingStatus.Delivered:
                return Result.Fail($"Order received");

            default:
                return Result.Fail($"Abnormal delivery status");
        }

        if (order.OrderStatus == OrderStatus.PaymentReceived)
            order.OrderStatus = OrderStatus.Shipping;

        var shipment = new Shipment
        {
            TrackingNumber = param.TrackingNumber,
            AdminComment = param.AdminComment,
            TotalWeight = param.TotalWeight,
            OrderId = id,
            UpdatedById = currentUser.Id,
            CreatedById = currentUser.Id,
            ShippedOn = DateTime.Now
        };

        foreach (var item in param.Items)
        {
            if (item.QuantityToShip <= 0
                || shipment.Items.Any(c => c.OrderItemId == item.OrderItemId)
                || !order.OrderItems.Any(c => c.Id == item.OrderItemId))
                continue;

            var orderItem = order.OrderItems.First(c => c.Id == item.OrderItemId);
            if (orderItem.ShippedQuantity >= orderItem.Quantity)
                throw new Exception($"Order Items[{orderItem.Product.Name}]，All shipped");
            if (orderItem.ShippedQuantity + item.QuantityToShip > orderItem.Quantity)
                throw new Exception($"Order Items[{orderItem.Product.Name}]，The quantity of shipment cannot>Order quantity");

            var shipmentItem = new ShipmentItem
            {
                Shipment = shipment,
                Quantity = item.QuantityToShip,
                ProductId = orderItem.ProductId,
                OrderItemId = item.OrderItemId,
                CreatedById = currentUser.Id,
                UpdatedById = currentUser.Id
            };
            shipment.Items.Add(shipmentItem);
            orderItem.ShippedQuantity += item.QuantityToShip;
        }

        if (!order.OrderItems.Any(c => c.ShippedQuantity < c.Quantity))
        {
            var timeFromMin = await _appSettingService.Get<int>(OrderKeys.OrderAutoCompleteTimeForMinute);

            //All Shipments
            order.ShippingStatus = ShippingStatus.Shipped;
            order.OrderStatus = OrderStatus.Shipped;
            order.ShippedOn = DateTime.Now;
            order.DeliveredEndOn = order.ShippedOn.Value.AddMinutes(timeFromMin);
            order.UpdatedOn = DateTime.Now;
        }

        _shipmentRepository.Add(shipment);

        using (var transaction = _orderRepository.BeginTransaction())
        {
            await _orderRepository.SaveChangesAsync();
            await _shipmentRepository.SaveChangesAsync();
            transaction.Commit();
        }

        return Result.Ok();
    }

    private async Task<OrderAddress> UserAddressToOrderAddress(int userAddressId, int userId, AddressType addressType,
        Order order)
    {
        var userAddress = await _userAddressRepository.Query()
            .Include(c => c.Address)
            .Where(c => c.Id == userAddressId && c.UserId == userId && c.AddressType == addressType)
            .FirstOrDefaultAsync();
        var shipping = userAddress ?? throw new Exception("The delivery address does not exist");
        var orderAddress = new OrderAddress()
        {
            Order = order,
            AddressType = shipping.AddressType,
            AddressLine1 = shipping.Address.AddressLine1,
            AddressLine2 = shipping.Address.AddressLine2,
            City = shipping.Address.City,
            Company = shipping.Address.Company,
            ContactName = shipping.Address.ContactName,
            CountryId = shipping.Address.CountryId,
            Email = shipping.Address.Email,
            Phone = shipping.Address.Phone,
            StateOrProvinceId = shipping.Address.StateOrProvinceId,
            ZipCode = shipping.Address.ZipCode
        };
        return orderAddress;
    }

    private async Task<OrderGetResult> GetOrder(int id, long no = 0)
    {
        var currentUser = await _workContext.GetCurrentUserAsync();
        Order order = null;
        var orderQuery = _orderRepository
            .Query()
            .Include(c => c.Customer)
            .Include(c => c.BillingAddress)
            .Include(c => c.ShippingAddress)
            .Include(c => c.OrderItems);
        if (id <= 0 && no <= 0)
            throw new Exception("Parameter abnormality");

        if (id > 0)
            order = await orderQuery.Where(c => c.Id == id).FirstOrDefaultAsync();
        else if (no > 0)
            order = await orderQuery.Where(c => c.No == no).FirstOrDefaultAsync();

        if (order == null)
            throw new Exception("The order does not exist");

        var result = new OrderGetResult
        {
            Id = order.Id,
            No = order.No.ToString(),
            AdminNote = order.AdminNote,
            BillingAddressId = order.BillingAddressId,
            CancelOn = order.CancelOn,
            CancelReason = order.CancelReason,
            CreatedById = order.CreatedById,
            CreatedOn = order.CreatedOn,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer.FullName,
            DiscountAmount = order.DiscountAmount,
            OrderNote = order.OrderNote,
            OrderStatus = order.OrderStatus,
            OrderTotal = order.OrderTotal,
            PaymentFeeAmount = order.PaymentFeeAmount,
            PaymentMethod = order.PaymentMethod,
            PaymentOn = order.PaymentOn,
            PaymentType = order.PaymentType,
            ShippingAddressId = order.ShippingAddressId,
            ShippingFeeAmount = order.ShippingFeeAmount,
            ShippingMethod = order.ShippingMethod,
            ShippingStatus = order.ShippingStatus,
            UpdatedById = order.UpdatedById,
            UpdatedOn = order.UpdatedOn,
            DeliveredOn = order.DeliveredOn,
            RefundStatus = order.RefundStatus,
            RefundReason = order.RefundReason,
            RefundOn = order.RefundOn,
            RefundAmount = order.RefundAmount,
            ShippedOn = order.ShippedOn
        };
        if (order.BillingAddress != null)
        {
            result.BillingAddress = new OrderGetAddressResult()
            {
                AddressLine1 = order.BillingAddress.AddressLine1,
                AddressLine2 = order.BillingAddress.AddressLine2,
                AddressType = order.BillingAddress.AddressType,
                City = order.BillingAddress.City,
                Company = order.BillingAddress.Company,
                ContactName = order.BillingAddress.ContactName,
                CountryId = order.BillingAddress.CountryId,
                Email = order.BillingAddress.Email,
                Id = order.BillingAddress.Id,
                Phone = order.BillingAddress.Phone,
                StateOrProvinceId = order.BillingAddress.StateOrProvinceId,
                ZipCode = order.BillingAddress.ZipCode
            };
            var provinces = await _countryService.GetProvinceByCache(order.BillingAddress.CountryId);
            IList<string> list = new List<string>();
            _countryService.ProvincesTransformToStringArray(provinces, order.BillingAddress.StateOrProvinceId,
                ref list);
            result.BillingAddress.StateOrProvinceIds = list;
        }

        if (order.ShippingAddress != null)
        {
            result.ShippingAddress = new OrderGetAddressResult()
            {
                AddressLine1 = order.ShippingAddress.AddressLine1,
                AddressLine2 = order.ShippingAddress.AddressLine2,
                AddressType = order.ShippingAddress.AddressType,
                City = order.ShippingAddress.City,
                Company = order.ShippingAddress.Company,
                ContactName = order.ShippingAddress.ContactName,
                CountryId = order.ShippingAddress.CountryId,
                Email = order.ShippingAddress.Email,
                Id = order.ShippingAddress.Id,
                Phone = order.ShippingAddress.Phone,
                StateOrProvinceId = order.ShippingAddress.StateOrProvinceId,
                ZipCode = order.ShippingAddress.ZipCode
            };
            var provinces = await _countryService.GetProvinceByCache(order.ShippingAddress.CountryId);
            IList<string> list = new List<string>();
            _countryService.ProvincesTransformToStringArray(provinces, order.ShippingAddress.StateOrProvinceId,
                ref list);
            result.ShippingAddress.StateOrProvinceIds = list;
        }

        foreach (var item in order.OrderItems)
            result.Items.Add(new OrderGetItemResult()
            {
                Id = item.ProductId,
                DiscountAmount = item.DiscountAmount,
                ItemAmount = item.ItemAmount,
                ItemWeight = item.ItemWeight,
                MediaUrl = item.ProductMediaUrl,
                Name = item.ProductName,
                Note = item.Note,
                ProductPrice = item.ProductPrice,
                Quantity = item.Quantity,
                ShippedQuantity = item.ShippedQuantity,
                OrderItemId = item.Id
            });

        return result;
    }

    /// <summary>
    /// Stock increase/stock decrease rules
    /// When increasing the order quantity, reduce the stock
    /// When decreasing the order quantity, increase the stock
    /// </summary>
    /// <param name="stocks"></param>
    /// <param name="product"></param>
    /// <param name="quantity">Reduce or increase the stock quantity, reduce the negative stock, increase the stock integer</param>
    /// <param name="orderId"></param>
    /// <param name="note"></param>
    private void OrderStockDoWorker(IList<Stock> stocks, IList<StockHistory> addStockHistories, Product product,
        User user, int quantity, Order order, string note)
    {
        if (product?.StockTrackingIsEnabled != true || quantity == 0)
            return;

        // For orders that have been cancelled or completed, modify the order quantity without changing the inventory
        var notStockOrderStatus = new OrderStatus[] { OrderStatus.Canceled, OrderStatus.Complete };
        if (order == null || notStockOrderStatus.Contains(order.OrderStatus))
            return;

        if (stocks.Count <= 0)
            throw new Exception("Product inventory does not exist");

        var productStocks = stocks.Where(c => c.ProductId == product.Id && c.IsEnabled);
        if (productStocks.Count() <= 0)
            throw new Exception($"merchandise：{product.Name}，No stock available");

        switch (product.StockReduceStrategy)
        {
            case StockReduceStrategy.PlaceOrderWithhold:
                //When placing an order to reduce inventory, support is successful without reducing inventory
                if (order.OrderStatus == OrderStatus.PaymentReceived)
                    return;
                break;

            case StockReduceStrategy.PaymentSuccessDeduct:
                //When payment reduces inventory, if the order is placed, payment is pending, or payment fails, the inventory will not be reduced
                var oss = new OrderStatus[] { OrderStatus.New, OrderStatus.PendingPayment, OrderStatus.PaymentFailed };
                if (oss.Contains(order.OrderStatus))
                    return;
                break;

            default:
                throw new Exception("Inventory deduction policy does not exist");
        }

        //Distributed lock, re-acquire inventory
        //todo

        if (quantity < 0)
        {
            //Decrease stock
            var absQuantity = Math.Abs(quantity);
            if (productStocks.Sum(c => c.StockQuantity) < absQuantity)
                throw new Exception($"merchandise[{product.Name}]Inventory shortage，Inventory remaining：{productStocks.Sum(c => c.StockQuantity)}");
            do
            {
                var firstStock = productStocks.Where(c => c.StockQuantity > 0).OrderBy(c => c.DisplayOrder)
                    .FirstOrDefault();
                if (firstStock == null)
                    throw new Exception($"Merchandise[{product.Name}]Inventory shortage");
                if (firstStock.StockQuantity >= absQuantity)
                {
                    firstStock.StockQuantity = firstStock.StockQuantity - absQuantity;
                    if (firstStock.StockQuantity < 0)
                        throw new Exception($"Merchandise[{product.Name}]Inventory shortage");
                    addStockHistories.Add(new StockHistory()
                    {
                        Note = $"Order：{order.No}，merchandise：{product.Name}，decrease stock：{absQuantity}。Note：{note}",
                        CreatedBy = user,
                        UpdatedBy = user,
                        AdjustedQuantity = -absQuantity,
                        StockQuantity = firstStock.StockQuantity,
                        WarehouseId = firstStock.WarehouseId,
                        ProductId = product.Id
                    });
                    absQuantity = 0;
                }
                else
                {
                    absQuantity = absQuantity - firstStock.StockQuantity;
                    if (absQuantity < 0)
                        throw new Exception($"Abnormal inventory deduction，Please try again");
                    addStockHistories.Add(new StockHistory()
                    {
                        Note = $"Order：{order.No}，merchandise：{product.Name}，decrease stock：{absQuantity}。Note：{note}",
                        CreatedBy = user,
                        UpdatedBy = user,
                        AdjustedQuantity = -firstStock.StockQuantity,
                        StockQuantity = 0,
                        WarehouseId = firstStock.WarehouseId,
                        ProductId = product.Id
                    });
                    firstStock.StockQuantity = 0;
                }
            } while (absQuantity > 0);
        }
        else if (quantity > 0)
        {
            //Increase inventory
            var firstStock = productStocks.OrderBy(c => c.DisplayOrder).FirstOrDefault();
            if (firstStock == null)
                throw new Exception($"merchandise：{product.Name}，No stock available");
            firstStock.StockQuantity += quantity;

            addStockHistories.Add(new StockHistory()
            {
                Note = $"Order：{order.No}，merchandise：{product.Name}，Increase inventory (reduce the number of items ordered): {quantity}. Note: {note}",
                CreatedBy = user,
                UpdatedBy = user,
                AdjustedQuantity = quantity,
                StockQuantity = firstStock.StockQuantity,
                WarehouseId = firstStock.WarehouseId,
                ProductId = product.Id
            });
        }
    }
}
