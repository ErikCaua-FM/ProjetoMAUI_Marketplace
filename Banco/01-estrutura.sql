-- VanderShop - estrutura do banco central (MySQL 8.4).
-- Execute este arquivo inteiro no banco vandershop.
-- Nomes e valores monetarios seguem os modelos atuais: valores em centavos.
-- Sem estoque. Sem dados de demonstracao.
-- IF NOT EXISTS permite repetir a criacao, mas nao atualiza tabelas existentes.

SET NAMES utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE `vandershop`;

CREATE TABLE IF NOT EXISTS `Usuarios` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Nome` VARCHAR(100) NOT NULL,
    `Documento` VARCHAR(20) NOT NULL,
    `Telefone` VARCHAR(20) NOT NULL,
    `Email` VARCHAR(150) NOT NULL,
    `Senha` VARCHAR(128) COLLATE utf8mb4_0900_as_cs NOT NULL,
    `CEP` VARCHAR(9) NOT NULL DEFAULT '',
    `Rua` VARCHAR(100) NOT NULL DEFAULT '',
    `Bairro` VARCHAR(60) NOT NULL DEFAULT '',
    `Numero` VARCHAR(10) NOT NULL DEFAULT '',
    `Complemento` VARCHAR(150) NOT NULL DEFAULT '',

    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_Usuarios_Documento` (`Documento`),
    UNIQUE KEY `UQ_Usuarios_Email` (`Email`),

    CONSTRAINT `CK_Usuarios_Nome`
        CHECK (CHAR_LENGTH(TRIM(`Nome`)) >= 3),

    CONSTRAINT `CK_Usuarios_Documento`
        CHECK (CHAR_LENGTH(TRIM(`Documento`)) >= 3),

    CONSTRAINT `CK_Usuarios_Telefone`
        CHECK (REGEXP_LIKE(`Telefone`, '^[0-9]{10,15}$', 'c')),

    CONSTRAINT `CK_Usuarios_Email`
        CHECK (CHAR_LENGTH(TRIM(`Email`)) >= 3),

    CONSTRAINT `CK_Usuarios_Senha`
        CHECK (
            CHAR_LENGTH(`Senha`) >= 8
            AND CHAR_LENGTH(TRIM(`Senha`)) > 0
        ),

    CONSTRAINT `CK_Usuarios_CEP`
        CHECK (
            `CEP` = ''
            OR REGEXP_LIKE(`CEP`, '^[0-9]{5}-[0-9]{3}$', 'c')
        )
) ENGINE=InnoDB
  DEFAULT CHARACTER SET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE IF NOT EXISTS `Categorias` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Categoria` VARCHAR(60) NOT NULL,

    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_Categorias_Categoria` (`Categoria`),

    CONSTRAINT `CK_Categorias_Categoria`
        CHECK (CHAR_LENGTH(TRIM(`Categoria`)) >= 2)
) ENGINE=InnoDB
  DEFAULT CHARACTER SET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE IF NOT EXISTS `Produtos` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Produto` VARCHAR(100) NOT NULL,
    `Descricao` TEXT NOT NULL,
    `CategoriaId` INT NOT NULL,
    `PrecoCentavos` BIGINT NOT NULL,
    `Ativo` BOOLEAN NOT NULL DEFAULT TRUE,

    PRIMARY KEY (`Id`),
    KEY `IX_Produtos_CategoriaId` (`CategoriaId`),

    CONSTRAINT `FK_Produtos_Categorias`
        FOREIGN KEY (`CategoriaId`) REFERENCES `Categorias` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `CK_Produtos_Produto`
        CHECK (CHAR_LENGTH(TRIM(`Produto`)) >= 2),

    CONSTRAINT `CK_Produtos_Descricao`
        CHECK (CHAR_LENGTH(TRIM(`Descricao`)) BETWEEN 1 AND 2000),

    CONSTRAINT `CK_Produtos_PrecoCentavos`
        CHECK (`PrecoCentavos` BETWEEN 0 AND 9007199254740991),

    CONSTRAINT `CK_Produtos_Ativo`
        CHECK (`Ativo` IN (0, 1))
) ENGINE=InnoDB
  DEFAULT CHARACTER SET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE IF NOT EXISTS `Pedidos` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ChaveLocal` VARCHAR(32) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `UsuarioId` INT NOT NULL,

    -- A API devera informar a data e hora em UTC.
    `DataHora` DATETIME(6) NOT NULL,

    `Status` VARCHAR(30) COLLATE utf8mb4_0900_as_cs
        NOT NULL DEFAULT 'Pedido Realizado',

    `TotalCentavos` BIGINT NOT NULL,
    `FormaPagamento` VARCHAR(30) COLLATE utf8mb4_0900_as_cs NOT NULL,
    `FormaRecebimento` VARCHAR(30) COLLATE utf8mb4_0900_as_cs NOT NULL,
    `CEP` VARCHAR(9) NOT NULL DEFAULT '',
    `Rua` VARCHAR(100) NOT NULL DEFAULT '',
    `Bairro` VARCHAR(60) NOT NULL DEFAULT '',
    `Numero` VARCHAR(10) NOT NULL DEFAULT '',
    `Complemento` VARCHAR(150) NOT NULL DEFAULT '',

    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_Pedidos_ChaveLocal` (`ChaveLocal`),
    KEY `IX_Pedidos_UsuarioId_DataHora` (`UsuarioId`, `DataHora`),
    KEY `IX_Pedidos_Status_DataHora` (`Status`, `DataHora`),
    KEY `IX_Pedidos_DataHora` (`DataHora`),

    CONSTRAINT `FK_Pedidos_Usuarios`
        FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `CK_Pedidos_ChaveLocal`
        CHECK (REGEXP_LIKE(`ChaveLocal`, '^[0-9a-f]{32}$', 'c')),

    CONSTRAINT `CK_Pedidos_Status`
        CHECK (`Status` IN (
            'Pedido Realizado',
            'Pedido Confirmado',
            'Em Preparação',
            'Pronto para Retirada',
            'Pedido Enviado',
            'Concluído',
            'Cancelado'
        )),

    CONSTRAINT `CK_Pedidos_TotalCentavos`
        CHECK (`TotalCentavos` BETWEEN 0 AND 9007199254740991),

    CONSTRAINT `CK_Pedidos_FormaPagamento`
        CHECK (`FormaPagamento` IN (
            'Pix',
            'Cartão de crédito',
            'Cartão de débito',
            'Boleto bancário'
        )),

    CONSTRAINT `CK_Pedidos_FormaRecebimento`
        CHECK (`FormaRecebimento` IN (
            'Retirar na loja',
            'Realizar uma entrega'
        )),

    CONSTRAINT `CK_Pedidos_Endereco`
        CHECK (
            (
                `FormaRecebimento` = 'Retirar na loja'
                AND `CEP` = ''
                AND `Rua` = ''
                AND `Bairro` = ''
                AND `Numero` = ''
                AND `Complemento` = ''
            )
            OR
            (
                `FormaRecebimento` = 'Realizar uma entrega'
                AND REGEXP_LIKE(`CEP`, '^[0-9]{5}-[0-9]{3}$', 'c')
                AND CHAR_LENGTH(TRIM(`Rua`)) > 0
                AND CHAR_LENGTH(TRIM(`Bairro`)) > 0
                AND CHAR_LENGTH(TRIM(`Numero`)) > 0
            )
        )
) ENGINE=InnoDB
  DEFAULT CHARACTER SET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE IF NOT EXISTS `ItensPedido` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `PedidoId` INT NOT NULL,

    -- Fica NULL se o produto for excluido do catalogo.
    `ProdutoId` INT NULL,

    -- Dados da compra preservados independentemente do catalogo.
    `NomeProduto` VARCHAR(100) NOT NULL,
    `Quantidade` INT NOT NULL,
    `PrecoUnitarioCentavos` BIGINT NOT NULL,
    `SubtotalCentavos` BIGINT NOT NULL,

    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_ItensPedido_Pedido_Produto` (`PedidoId`, `ProdutoId`),
    KEY `IX_ItensPedido_ProdutoId` (`ProdutoId`),

    CONSTRAINT `FK_ItensPedido_Pedidos`
        FOREIGN KEY (`PedidoId`) REFERENCES `Pedidos` (`Id`)
        ON DELETE CASCADE
        ON UPDATE RESTRICT,

    CONSTRAINT `FK_ItensPedido_Produtos`
        FOREIGN KEY (`ProdutoId`) REFERENCES `Produtos` (`Id`)
        ON DELETE SET NULL
        ON UPDATE RESTRICT,

    CONSTRAINT `CK_ItensPedido_NomeProduto`
        CHECK (CHAR_LENGTH(TRIM(`NomeProduto`)) >= 2),

    CONSTRAINT `CK_ItensPedido_Quantidade`
        CHECK (`Quantidade` > 0),

    CONSTRAINT `CK_ItensPedido_PrecoUnitarioCentavos`
        CHECK (`PrecoUnitarioCentavos` BETWEEN 0 AND 9007199254740991),

    CONSTRAINT `CK_ItensPedido_SubtotalCentavos`
        CHECK (`SubtotalCentavos` BETWEEN 0 AND 9007199254740991),

    CONSTRAINT `CK_ItensPedido_CalculoSubtotal`
        CHECK (
            `SubtotalCentavos` =
                CAST(`PrecoUnitarioCentavos` AS DECIMAL(30, 0)) * `Quantidade`
        )
) ENGINE=InnoDB
  DEFAULT CHARACTER SET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci;