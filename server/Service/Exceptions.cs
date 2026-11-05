namespace Service;

public abstract class ServiceException(string message) : Exception(message);

public sealed class ValidationFailedException(string message) : ServiceException(message); // 400
public sealed class ForbiddenException(string message) : ServiceException(message);        // 403
public sealed class NotFoundException(string message) : ServiceException(message);         // 404
public sealed class ConflictException(string message) : ServiceException(message);         // 409
