using System.Text.Json;
using Domain;
using DTO.External.RabbitMQ;
using DTO.External.RabbitMQ.Mappers;
using Microsoft.Extensions.Logging;

namespace External.RabbitMQ;

public class EmailCommandReader
{
    private readonly EmailCommandDtoMapper _mapper;
    private readonly ILogger<EmailCommandReader> _logger;

    public EmailCommandReader(EmailCommandDtoMapper mapper, ILogger<EmailCommandReader> logger)
    {
        _mapper = mapper;
        _logger = logger;
    }

    public SendTemplatedEmailRequest? Read(string routingKey, ReadOnlyMemory<byte> body)
    {
        EmailCommandDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<EmailCommandDto>(body.Span);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Rejecting message: invalid JSON contract. RoutingKey: {RoutingKey}", routingKey);
            return null;
        }

        var route = routingKey.Split('.');
        if (dto is null || route.Length != 3 || route.Any(string.IsNullOrWhiteSpace)
            || route[0] != dto.Source || route[1] != dto.Tenant || route[2] != dto.Action
            || string.IsNullOrWhiteSpace(dto.ContentVersion) || dto.ContentVersion.Split('.')[0] != "1"
            || dto.Content is null)
        {
            _logger.LogWarning("Rejecting message: invalid envelope, routing key or content. RoutingKey: {RoutingKey}", routingKey);
            return null;
        }

        var request = _mapper.Map(dto with { RoutingKey = routingKey });
        if (request is null)
        {
            _logger.LogWarning("Rejecting message: expected content.model object or flat content.email. RoutingKey: {RoutingKey}", routingKey);
        }
        return request;
    }
}
