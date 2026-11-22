namespace RoadMate.Application.Common;

public class ForeignKeyViolationException(Exception? inner = null) : Exception("Vi phạm khoá ngoại.", inner);
