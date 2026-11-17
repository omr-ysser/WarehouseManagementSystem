using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Warehouse.Application.DTOs;

public sealed record CountryResponse(
    string Code,
    string Name);

public static class CustomerCountries
{
    // Uses the country/region data available in the runtime.
    public static IReadOnlyList<CountryResponse> All { get; } =
        CultureInfo
            .GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name))
            .Where(region => region.TwoLetterISORegionName.Length == 2)
            .DistinctBy(region => region.TwoLetterISORegionName)
            .Select(region => new CountryResponse(
                region.TwoLetterISORegionName,
                region.EnglishName))
            .OrderBy(country => country.Name)
            .ToArray();

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var code = value.Trim().ToUpperInvariant();

        return All.Any(country => country.Code == code);
    }
}

public sealed class CountryCodeAttribute : ValidationAttribute
{
    public CountryCodeAttribute()
        : base("Please select a valid nationality.")
    {
    }

    public override bool IsValid(object? value)
    {
        // RequiredAttribute handles missing values.
        if (value is null || value is string { Length: 0 })
        {
            return true;
        }

        return value is string code &&
               CustomerCountries.IsValid(code);
    }
}
