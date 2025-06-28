using MediatR;

public class DeleteMapPointCommand : IRequest<bool>
{
    public int Id { get; set; }
}
