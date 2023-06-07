using BikeSite.Dal;
using BikeSite.Dal.Models;
using BikeSite.Domain;
using Microsoft.EntityFrameworkCore;

namespace BikeSite.Service;

public interface IBikeService
{
    Task<OperationResult<List<BikeDto>>> GetBikes();
    Task<OperationResult<BikeDto>> CreateOrUpdateBike(BikeDto domain);
    Task<OperationResult<int>> RemoveBike(Guid bikeId);
}

public class BikeService : IBikeService
{
    private readonly ApplicationDbContext _dbContext;

    public BikeService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<OperationResult<List<BikeDto>>> GetBikes()
    {
        try
        {
            var bikes = _dbContext.Bikes.Include(b => b.Brand).AsNoTracking();
            return new OperationResult<List<BikeDto>>()
            {
                Success = true,
                Value = await bikes.Select(x => BikeDto.FromModel(x)).ToListAsync()
            };
        }
        catch (Exception e)
        {
            return new OperationResult<List<BikeDto>>()
            {
                Success = false,
                Error = e.ToString()
            };
        }
    }

    public async Task<OperationResult<BikeDto>> CreateOrUpdateBike(BikeDto domain)
    {
        try
        {
            if (domain.BikeId.HasValue)
            {
                //Update
                var bike = _dbContext.Bikes.FirstOrDefault(x => x.BikeId == domain.BikeId.Value);
                if (bike == null)
                {
                    return new OperationResult<BikeDto>()
                    {
                        Success = false,
                        Error = $"Bike {domain.BikeId.Value} was not found"
                    };
                }

                if (bike.Brand.BrandId != domain.Brand.BikeBrandId)
                {
                    var brand = _dbContext.BikeBrands.FirstOrDefault(x => x.BrandId == domain.Brand.BikeBrandId);
                    if (brand == null)
                    {
                        return new OperationResult<BikeDto>()
                        {
                            Success = false,
                            Error = $"Brand {domain.Brand.BikeBrandId} was not found"
                        };
                    }
                    bike.BikeBrandId = brand.Id;
                }

                bike.Model = domain.Model;
                bike.Price = domain.Price;
                return new OperationResult<BikeDto>()
                {
                    Success = true,
                    Value = BikeDto.FromModel(bike)
                };
            }
            else //insert
            {
                var brand = _dbContext.BikeBrands.FirstOrDefault(x => x.BrandId == domain.Brand.BikeBrandId);
                if (brand == null)
                {
                    return new OperationResult<BikeDto>()
                    {
                        Success = false,
                        Error = $"Brand {domain.Brand.BikeBrandId} was not found"
                    };
                }
                var bike = new Bike()
                {
                    BikeId = new Guid(),
                    FrameSize = domain.FrameSize,
                    Model = domain.Model,
                    BikeBrandId = brand.Id
                };
                var entity = await _dbContext.Bikes.AddAsync(bike);
                await _dbContext.SaveChangesAsync();
                return new OperationResult<BikeDto>()
                {
                    Success = true,
                    Value = BikeDto.FromModel(entity.Entity)
                };
            }
        }
        catch (Exception e)
        {
            return new OperationResult<BikeDto>()
            {
                Success = false,
                Error = e.ToString()
            };
        }
    }
    
    public async Task<OperationResult<int>> RemoveBike(Guid bikeId)
    {
        try
        {
            var bike = _dbContext.Bikes.FirstOrDefault(x => x.BikeId == bikeId);
            if (bike == null)
            {
                return new OperationResult<int>()
                {
                    Success = false,
                    Error = $"Bike {bikeId} was not found"
                };
            }

            _dbContext.Bikes.Remove(bike);
            await _dbContext.SaveChangesAsync();
            return new OperationResult<int>()
            {
                Success = false,
                Value = 0
            };
        }
        catch (Exception e)
        {
            return new OperationResult<int>()
            {
                Success = false,
                Error = e.ToString()
            };
        }
    }
}