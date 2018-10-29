using Domain.Entities;

namespace Domain.DTO
{
    public class QuestaoDTO
    {
        public QuestaoDTO()
        {

        }

        public QuestaoDTO(Questao questao)
        {
            Id = questao.Id;
            Titulo = questao.Titulo;
            NotaMaxima = questao.NotaMaxima;
        }

        public QuestaoDTO(Questao questao, Resposta resposta)
        {
            Id = questao.Id;
            Titulo = questao.Titulo;
            NotaMaxima = questao.NotaMaxima;

            if (resposta != null)
            {
                Resposta = resposta.Conteudo;
                PontosGanhos = resposta.PontosGanhos;
            }
        }

        public int? Id { get; set; }
        public string Titulo { get; set; }
        public int NotaMaxima { get; set; }
        public string Resposta { get; set; }
        public int? PontosGanhos { get; set; }

        public void PreencherEntidade(Questao questao)
        {
            questao.Titulo = Titulo;
            questao.NotaMaxima = NotaMaxima;
        }
    }
}