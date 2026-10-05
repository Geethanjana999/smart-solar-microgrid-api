/*
 * File        : DashboardService.cs
 * Description : Computes dashboard counts; kept in the service layer per the fat-service pattern.
 */
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Data;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class DashboardService(MongoContext db) : IDashboardService
{
    // Counts stations, open slots/energy, and pending / approved-future / completed reservations.
    public async Task<DashboardResponse> GetAsync(string? nic)
    {
        var now = DateTime.UtcNow;
        var stations = await db.Stations.CountDocumentsAsync(x => x.IsActive);
        var openSlots = await db.Slots.Find(x => x.IsActive && x.EndTime > now).ToListAsync();

        var reservationFilter = nic == null
            ? Builders<EnergyReservation>.Filter.Empty
            : Builders<EnergyReservation>.Filter.Eq(x => x.ProsumerNic, nic);
        var reservations = await db.Reservations.Find(reservationFilter).ToListAsync();

        var pending = reservations.Count(x => x.Status == ReservationStatus.Pending);
        var completed = reservations.Count(x => x.Status == ReservationStatus.Completed);

        // Approved future = confirmed reservations whose slot has not started yet.
        var confirmedSlotIds = reservations.Where(x => x.Status == ReservationStatus.Confirmed).Select(x => x.SlotId).Distinct().ToList();
        var futureSlotIds = confirmedSlotIds.Count == 0
            ? new HashSet<string>()
            : (await db.Slots.Find(x => confirmedSlotIds.Contains(x.Id) && x.StartTime > now).ToListAsync())
                .Select(x => x.Id).ToHashSet();
        var approvedFuture = reservations.Count(x => x.Status == ReservationStatus.Confirmed && futureSlotIds.Contains(x.SlotId));

        return new DashboardResponse(
            (int)stations, openSlots.Count, pending, approvedFuture, completed, openSlots.Sum(x => x.AvailableKwh));
    }
}
