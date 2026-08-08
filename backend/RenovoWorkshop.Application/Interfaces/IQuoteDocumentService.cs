using RenovoWorkshop.Domain.Entities;

namespace RenovoWorkshop.Application.Interfaces;

public interface IQuoteDocumentService
{
    // Gera o PDF do orçamento (peças, serviços, totais, aprovação por WhatsApp e
    // rodapé institucional) a partir dos dados já preenchidos na OS — sem depender
    // de nenhum documento fiscal.
    byte[] GenerateQuotePdf(ServiceOrder order, Customer customer);
}
