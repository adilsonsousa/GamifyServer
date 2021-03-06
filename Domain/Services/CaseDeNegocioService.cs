using System;
using System.Collections.Generic;
using System.Linq;
using Domain.DTO;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;

namespace Domain.Services
{
    public class CaseDeNegocioService : BaseService<CaseDeNegocio>, ICaseDeNegocioService
    {
        private readonly ICaseDeNegocioRepository _caseDeNegocioRepository;
        private readonly IAlunoDoCaseRepository _alunoDoCaseRepository;
        private readonly IConsultaDeAlunosService _consultaDeAlunosService;

        public CaseDeNegocioService(ICaseDeNegocioRepository caseDeNegocioRepository, IAlunoDoCaseRepository alunoDoCaseRepository,
            IConsultaDeAlunosService consultaDeAlunosService)
            : base(caseDeNegocioRepository)
        {
            _caseDeNegocioRepository = caseDeNegocioRepository;
            _alunoDoCaseRepository = alunoDoCaseRepository;
            _consultaDeAlunosService = consultaDeAlunosService;
        }

        public int Adicionar(CaseDetalhesDTO caseDTO, Usuario usuarioLogado)
        {
            if (caseDTO == null || caseDTO.Id > 0)
                throw new Exception("Solicitação inválida.");

            if (!ExisteUsuarioLogado(usuarioLogado))
                throw new Exception("É necessário um usuário autenticado para realizar esta ação.");

            CaseDeNegocio caseDeNegocio = new CaseDeNegocio();
            caseDeNegocio.IdProfessor = usuarioLogado.Id;
            caseDeNegocio.Professor = usuarioLogado;

            caseDTO.PreencherEntidade(caseDeNegocio);

            caseDeNegocio.GerarChaveDeBusca();

            Adicionar(caseDeNegocio);

            return caseDeNegocio.Id;
        }

        public void Atualizar(CaseDetalhesDTO caseDTO, Usuario usuario)
        {
            if (caseDTO == null || !caseDTO.Id.HasValue)
                throw new Exception("Solicitação inválida.");

            if (!ExisteUsuarioLogado(usuario))
                throw new Exception("É necessário um usuário autenticado para realizar esta ação.");

            CaseDeNegocio caseDeNegocio = ObterPorId(caseDTO.Id.Value);

            if (caseDeNegocio == null)
                throw new Exception("Case de negócio não encontrado.");

            if (caseDeNegocio.Professor != usuario)
                throw new Exception("Somente o professor pode atualizar os dados.");

            caseDTO.PreencherEntidade(caseDeNegocio);

            Atualizar(caseDeNegocio);
        }

        public IEnumerable<CaseDTO> ListarCasesDeNegocioAssociadosAoUsuario(int idUsuario)
        {
            List<CaseDTO> response = new List<CaseDTO>();
            AdicionarCasesDeNegociosAssociadosComoProfessor(response, idUsuario);
            AdicionarCasesDeNegociosAssociadosComoAluno(response, idUsuario);

            return response.OrderBy(c => c.Nome).ToList();
        }

        public CaseDetalhesDTO ObterDetalhesPorId(int idCaseDeNegocio, Usuario usuario)
        {
            var caseDeNegocio = ObterPorId(idCaseDeNegocio);

            if (caseDeNegocio == null)
                throw new Exception("Case de negócio não encontrado.");

            var response = new CaseDetalhesDTO(caseDeNegocio);

            PreencherPermissoesDoUsuario(response, usuario, caseDeNegocio);

            return response;
        }

        public void InscreverUsuarioNoCaseDeNegocio(int idCaseDeNegocio, Usuario usuario)
        {
            var caseDeNegocio = ObterPorId(idCaseDeNegocio);

            if (!PermiteUsuarioSeInscreverNoCaseDeNegocio(usuario, caseDeNegocio))
                throw new Exception("Usuário não pode se inscrever neste case de negócio");

            var alunoDoCase = new AlunoDoCase(caseDeNegocio, usuario);
            _alunoDoCaseRepository.Add(alunoDoCase);
        }

        public int? Localizar(LocalizarCaseRequest request)
        {
            return _caseDeNegocioRepository.Queryable()
                .Where(c => c.ChaveDeBusca == request.ChaveDeBusca)
                .Select(c => (int?)c.Id)
                .FirstOrDefault();
        }

        #region Métodos privados

        private void AdicionarCasesDeNegociosAssociadosComoProfessor(List<CaseDTO> lista, int idUsuario)
        {
            lista.AddRange(
                _caseDeNegocioRepository
                    .Queryable()
                    .Where(c => c.Professor.Id == idUsuario)
                    .Select(c => new CaseDTO
                    {
                        Id = c.Id,
                        Nome = c.Nome
                    }).ToList()
            );
        }

        private void AdicionarCasesDeNegociosAssociadosComoAluno(List<CaseDTO> lista, int idUsuario)
        {
            lista.AddRange(
                (from c in _caseDeNegocioRepository.Queryable()
                 join a in _alunoDoCaseRepository.Queryable() on c.Id equals a.IdCaseDeNegocio
                 where a.IdUsuario == idUsuario
                 select new CaseDTO
                 {
                     Id = c.Id,
                     Nome = c.Nome
                 }).ToList()
            );
        }

        private void PreencherPermissoesDoUsuario(CaseDetalhesDTO response, Usuario usuario, CaseDeNegocio caseDeNegocio)
        {
            response.PermiteEditar = false;
            response.Inscrito = false;
            response.PermiteSeInscrever = false;
            response.PermiteCriarNovoGrupo = false;
            response.PermiteCriarLicoes = false;
            response.PermiteCriarTrofeus = false;

            if (ExisteUsuarioLogado(usuario) && ExisteCaseDeNegocio(caseDeNegocio))
            {
                if (UsuarioEstaAssociadoAoCaseDeNegocioComoProfessor(usuario, caseDeNegocio))
                {
                    response.PermiteEditar = true;
                    response.PermiteCriarLicoes = true;
                    response.PermiteCriarTrofeus = true;
                }
                else if (UsuarioEstaInscritoNoCaseDeNegocio(usuario, caseDeNegocio))
                {
                    response.Inscrito = true;
                    if (caseDeNegocio.PermiteMontarGrupos)
                    {
                        bool jaPossuiGrupo = _consultaDeAlunosService.UsuarioFazParteDeAlgumGrupoDoCaseDeNegocio(usuario.Id, caseDeNegocio.Id);
                        response.PermiteCriarNovoGrupo = !jaPossuiGrupo;
                    }
                }
                else
                    response.PermiteSeInscrever = true;
            }
        }

        private bool PermiteUsuarioSeInscreverNoCaseDeNegocio(Usuario usuario, CaseDeNegocio caseDeNegocio)
        {
            return ExisteUsuarioLogado(usuario)
                && ExisteCaseDeNegocio(caseDeNegocio)
                && !UsuarioEstaAssociadoAoCaseDeNegocioComoProfessor(usuario, caseDeNegocio)
                && !UsuarioEstaInscritoNoCaseDeNegocio(usuario, caseDeNegocio);
        }

        private bool ExisteUsuarioLogado(Usuario usuario)
        {
            return usuario != null;
        }

        private bool ExisteCaseDeNegocio(CaseDeNegocio caseDeNegocio)
        {
            return caseDeNegocio != null;
        }

        public bool PermiteUsuarioEditarCaseDeNegocio(Usuario usuario, CaseDeNegocio caseDeNegocio)
        {
            return UsuarioEstaAssociadoAoCaseDeNegocioComoProfessor(usuario, caseDeNegocio);
        }

        public bool UsuarioEstaAssociadoAoCaseDeNegocioComoProfessor(Usuario usuario, CaseDeNegocio caseDeNegocio)
        {
            return ExisteUsuarioLogado(usuario) && ExisteCaseDeNegocio(caseDeNegocio) && caseDeNegocio.Professor == usuario;
        }

        public bool UsuarioEstaInscritoNoCaseDeNegocio(Usuario usuario, CaseDeNegocio caseDeNegocio)
        {
            return _alunoDoCaseRepository.UsuarioEstaAssociadoAoCaseDeNegocio(usuario.Id, caseDeNegocio.Id);
        }

        #endregion
    }
}