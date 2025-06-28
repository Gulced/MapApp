using MediatR;

public class UpdateMapPointCommand : IRequest<bool>
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}
