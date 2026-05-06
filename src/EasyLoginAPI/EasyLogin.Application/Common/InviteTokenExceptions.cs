namespace EasyLogin.Application.Common;

public class InviteTokenExpiredException(string message) : Exception(message);

public class InviteTokenUsedException(string message) : Exception(message);

public class InviteAlreadyPendingException(string message) : Exception(message);
