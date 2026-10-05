/*
 * File        : QrVerifyRequest.cs
 * Description : Payload an operator submits after scanning a prosumer's QR code.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class QrVerifyRequest
{
    [Required]
    public string QrToken { get; set; } = "";
}
