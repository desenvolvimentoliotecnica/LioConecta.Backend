namespace LioConecta.Infrastructure.Seed;

internal static class ProfileSeedContent
{
    internal const string LeonardoSabinoMendesSkillsJson =
        """
        [
          {"name":".NET","level":5,"endorsements":12},
          {"name":"React","level":4,"endorsements":9},
          {"name":"Integrações","level":4,"endorsements":7},
          {"name":"SQL Server","level":4,"endorsements":6}
        ]
        """;

    internal const string LeonardoSabinoMendesPersonalDataJson =
        """
        {
          "bio": "Leonardo Sabino Mendes atua em Sistemas na LioConecta, com foco em desenvolvimento e integrações.",
          "aboutMe": "Sou Leonardo, desenvolvedor sênior na área de Sistemas. Trabalho na LioTécnica integrando soluções internas e TOTVS RM.",
          "pronouns": "Ele/Dele",
          "fullName": "Leonardo Sabino Mendes",
          "birthDate": "20 de novembro de 1990",
          "birthMonth": 11,
          "birthDay": 20,
          "visibility": "public",
          "availability": {
            "workModel": "Híbrido",
            "schedule": "9h–18h",
            "timezone": "America/Sao_Paulo",
            "floor": "2º andar",
            "room": "Sistemas · Sala 204"
          },
          "roleTenure": {
            "years": 4,
            "since": "mar de 2022",
            "title": "Desenvolvedor Sr."
          },
          "languages": [
            {"name": "Português", "level": "Nativo"},
            {"name": "Inglês", "level": "Intermediário"}
          ]
        }
        """;

    internal const string MariaSilvaSkillsJson =
        """
        [
          {"name":"Roadmap","level":4,"endorsements":18},
          {"name":"Discovery","level":5,"endorsements":21},
          {"name":"UX Research","level":3,"endorsements":24},
          {"name":"Gerente","level":4,"endorsements":7}
        ]
        """;

    internal const string MariaSilvaPersonalDataJson =
        """
        {
          "bio": "Maria Silva atua em Produto na LioConecta, com foco em gerente de projetos e colaboração entre áreas.",
          "aboutMe": "Sou Maria, gerente de projetos na área de Produto. Trabalho na LioTécnica desde mar de 2022 e gosto de conectar pessoas, compartilhar conhecimento e entregar resultados com impacto.",
          "pronouns": "Ela/Dela",
          "fullName": "Maria Silva",
          "birthDate": "26 de fevereiro de 1985",
          "birthMonth": 2,
          "birthDay": 26,
          "cpf": "***.***.***-33",
          "rg": "64.033.933-3",
          "maritalStatus": "Casado(a)",
          "nationality": "Brasileira",
          "visibility": "public",
          "availability": {
            "workModel": "Híbrido",
            "schedule": "9h–18h",
            "timezone": "America/Sao_Paulo",
            "floor": "3º andar",
            "room": "Produto · Sala 313"
          },
          "links": {
            "linkedin": "https://www.linkedin.com/in/maria-silva"
          },
          "roleTenure": {
            "years": 4,
            "since": "mar de 2022",
            "title": "Gerente de Projetos"
          },
          "languages": [
            {"name": "Português", "level": "Nativo"},
            {"name": "Inglês", "level": "Intermediário"}
          ],
          "education": [
            {
              "degree": "Gestão de Produtos Digitais",
              "institution": "PM3",
              "period": "2013–2015",
              "type": "certificacao",
              "note": "Certificação"
            },
            {
              "degree": "Design Digital",
              "institution": "IAC",
              "period": "2010–2014",
              "type": "graduacao",
              "note": "Graduação"
            }
          ],
          "certifications": [
            {
              "name": "Certified Scrum Product Owner",
              "issuer": "Scrum Alliance",
              "year": "2024",
              "type": "certificacao"
            }
          ],
          "history": [
            {
              "date": "2022",
              "title": "Gerente de Projetos",
              "dept": "Produto",
              "type": "atual",
              "note": "Cargo atual na LioConecta."
            },
            {
              "date": "2020",
              "title": "Analista de Projetos",
              "dept": "Produto",
              "type": "promotion",
              "note": "Promoção após entrega do programa de transformação digital."
            }
          ],
          "stats": {
            "tenureYears": 4,
            "directReports": 0,
            "groups": 2,
            "recognitions": 3,
            "projectsCount": 2
          },
          "groups": [
            {
              "name": "Produto & Inovação",
              "role": "Membro",
              "members": 18,
              "url": "/grupos/meus-grupos"
            },
            {
              "name": "Agile Guild",
              "role": "Facilitadora",
              "members": 42,
              "url": "/grupos/meus-grupos"
            }
          ]
        }
        """;
}
