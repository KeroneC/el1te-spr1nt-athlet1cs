using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSquareCheckoutOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckoutAttemptId",
                table: "Orders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CheckoutPayloadHash",
                table: "Orders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckoutReturnTokenExpiresAtUtc",
                table: "Orders",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CheckoutReturnTokenHash",
                table: "Orders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CustomerCancellationExpiresAtUtc",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CustomerCancellationRequestedAtUtc",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaymentVerifiedAtUtc",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SquarePaymentLinkDeletedAtUtc",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SquarePaymentLinkUrl",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ActorUserId",
                table: "CommerceRefunds",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.Sql(
                """
                UPDATE [Orders]
                SET [CheckoutAttemptId] = CONCAT('LEGACY-', REPLACE(CONVERT(nvarchar(36), [Id]), '-', '')),
                    [CheckoutPayloadHash] = REPLICATE('0', 64),
                    [CheckoutReturnTokenHash] = CONCAT(REPLICATE('0', 32), REPLACE(CONVERT(nvarchar(36), [Id]), '-', '')),
                    [CheckoutReturnTokenExpiresAtUtc] = COALESCE([UpdatedAt], [CreatedAt])
                WHERE [CheckoutAttemptId] = '';
                """);

            migrationBuilder.CreateTable(
                name: "CommerceRefundLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommerceRefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    RestockQuantity = table.Column<int>(type: "int", nullable: false),
                    InventoryAdjustmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceRefundLines", x => x.Id);
                    table.CheckConstraint("CK_CommerceRefundLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_CommerceRefundLines_RestockQuantity", "[RestockQuantity] >= 0 AND [RestockQuantity] <= [Quantity]");
                    table.ForeignKey(
                        name: "FK_CommerceRefundLines_CommerceRefunds_CommerceRefundId",
                        column: x => x.CommerceRefundId,
                        principalTable: "CommerceRefunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommerceRefundLines_InventoryAdjustments_InventoryAdjustmentId",
                        column: x => x.InventoryAdjustmentId,
                        principalTable: "InventoryAdjustments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommerceRefundLines_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "Body",
                value: "Last updated: August 8, 2026\n\n## Information we collect\n\nWhen you contact El1te Spr1nt Athlet1cs, we collect the name, email address, optional phone number, inquiry details, and message you provide. Store checkout collects the adult buyer's name, email, phone number, product configuration, order history, practice or event handoff status, cancellations, and refunds.\n\n## Payments and fulfillment\n\nCard details are entered on Square and are never received or stored by El1te. Square receives the information required to process payment, configured taxes, receipts, cancellations, and refunds.\n\nAzure hosts the application and supports transactional email. Authorized staff may access submitted information only for club operations, support, security, accounting, safety, and legal obligations.\n\n## Browser storage and analytics\n\nThe public cart stores only non-personal product configuration in the browser. Essential security and checkout-return cookies may be used. Public performance analytics are cookie-free and exclude names, emails, addresses, form contents, cart details, and Admin activity.\n\nWe do not sell or rent personal information.\n\n## Retention and security\n\nRecords are kept only as reasonably needed for fulfillment, accounting, disputes, safety, security, and legal obligations. We use reasonable safeguards, but no internet service can promise absolute security.\n\n## Children and families\n\nStore purchases must be made by an adult. A parent or guardian should submit information involving a youth athlete.\n\n## Your choices\n\nTo request access, correction, or deletion, email [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com). Some records may need to be retained for accounting, safety, dispute, or legal reasons.");

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "Body",
                value: "Last updated: August 8, 2026\n\n## Our commitment\n\nEl1te Spr1nt Athlet1cs is working toward WCAG 2.2 Level AA so the website can be used by as many people as possible.\n\nOur ongoing work includes:\n\n- Keyboard access and visible focus\n- Screen-reader support and meaningful alternative text\n- Browser zoom and responsive layouts\n- Reduced-motion support\n- Clear labels, instructions, validation, and error messages\n\n## Third-party services\n\nSquare provides the hosted payment experience. We do not control every part of that third-party service, but we will help identify a practical alternative when possible.\n\n## Report a barrier\n\nEmail [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com) with the page, device or browser, approximate time, and a description of the problem. Please do not include payment information or sensitive athlete information.\n\nWe will investigate and respond as soon as practical. Accessibility is an ongoing responsibility, and this statement will be updated as the website and its services change.");

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000010"),
                column: "Body",
                value: "Last updated: August 8, 2026\n\n## Using this website\n\nBy using this website, you agree to these terms and, when making a purchase, the Store Policy. Club schedules, programs, eligibility, prices, inventory, and other content may change as information is reviewed.\n\nStore buyers must be at least 18 years old and must provide accurate contact, payment, and fulfillment information.\n\n## Acceptable use\n\nDo not attempt unauthorized access, disrupt the service, impersonate another person, submit information you are not authorized to provide, use automated requests that place an unreasonable burden on the service, or use club or sponsor intellectual property without permission.\n\n## Content and third parties\n\nEl1te owns or is authorized to use the website's content and branding. Viewing the website does not grant permission to copy or reuse that material.\n\nSquare, sponsors, and linked websites operate under their own terms and policies. We will correct known website errors where practical, but we cannot promise uninterrupted service or guarantee a third party's availability or performance.\n\n## Questions and updates\n\nThese limitations apply only to the extent permitted by law. We may update these terms as the website changes. Questions may be sent to [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com).");

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000011"),
                column: "Body",
                value: "Last updated: August 8, 2026\n\n## Prices, payment, and handoff\n\nPrices are in U.S. dollars. Square processes payment and calculates configured taxes. Card details are not stored by El1te.\n\nAll products are delivered through an arranged practice or event handoff. Staff do not ship launch-store orders.\n\n## Review and cancellation\n\nReview the size, color, approved design, name, and number choices before payment. You may cancel the complete order and receive a full Square refund from the secure order-status page during the 30 minutes following confirmed payment. After that deadline, cancellation requires staff review and is not guaranteed.\n\n## Returns and product problems\n\nCorrectly produced name or number items are final sale after the cancellation window, except when an item is damaged, defective, or produced incorrectly by the club.\n\nUnworn, unwashed non-personalized products in original condition may be returned or exchanged within 14 days of handoff, subject to available stock.\n\nRefunds return to the original Square payment method. Processing time after a refund is submitted depends on the buyer's financial institution.\n\n## Production and available choices\n\nProduction and handoff dates are estimates. Staff will contact the buyer when an order is ready for its arranged handoff.\n\nLaunch products offer only listed size, garment-color, approved-design, name, and number choices. Custom artwork and broader special requests are not available.\n\n## Store support\n\nEmail [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com) with your order reference. Do not send card details by email.");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CheckoutAttemptId",
                table: "Orders",
                column: "CheckoutAttemptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CheckoutReturnTokenHash",
                table: "Orders",
                column: "CheckoutReturnTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommerceRefundLines_CommerceRefundId_OrderItemId",
                table: "CommerceRefundLines",
                columns: new[] { "CommerceRefundId", "OrderItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommerceRefundLines_InventoryAdjustmentId",
                table: "CommerceRefundLines",
                column: "InventoryAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommerceRefundLines_OrderItemId",
                table: "CommerceRefundLines",
                column: "OrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommerceRefundLines");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CheckoutAttemptId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CheckoutReturnTokenHash",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CheckoutAttemptId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CheckoutPayloadHash",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CheckoutReturnTokenExpiresAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CheckoutReturnTokenHash",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerCancellationExpiresAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerCancellationRequestedAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentVerifiedAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SquarePaymentLinkDeletedAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SquarePaymentLinkUrl",
                table: "Orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "ActorUserId",
                table: "CommerceRefunds",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "Body",
                value: "Last updated: August 4, 2026\n\n## Information we collect\n\nWhen you contact El1te Spr1nt Athlet1cs, we collect the name, email address, optional phone number, inquiry details, and message you provide. Store checkout collects the adult buyer's name, email, phone number, delivery address when shipping is required, product selections, order history, fulfillment status, tracking, cancellations, and refunds.\n\n## Payments and fulfillment\n\nCard details are entered on Square and are never received or stored by El1te. Printify receives the customer and order information required to manufacture and deliver Printify items. Delivery carriers receive the information required to deliver packages.\n\nAzure hosts the application and supports transactional email. Authorized staff may access submitted information only for club operations, support, security, accounting, safety, and legal obligations.\n\n## Browser storage and analytics\n\nThe public cart stores only non-personal product configuration in the browser. Essential security and checkout-return cookies may be used. Public performance analytics are cookie-free and exclude names, emails, addresses, form contents, cart details, and Admin activity.\n\nWe do not sell or rent personal information.\n\n## Retention and security\n\nRecords are kept only as reasonably needed for fulfillment, accounting, disputes, safety, security, and legal obligations. We use reasonable safeguards, but no internet service can promise absolute security.\n\n## Children and families\n\nStore purchases must be made by an adult. A parent or guardian should submit information involving a youth athlete.\n\n## Your choices\n\nTo request access, correction, or deletion, email [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com). Some records may need to be retained for accounting, safety, dispute, or legal reasons.");

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "Body",
                value: "Last updated: August 4, 2026\n\n## Our commitment\n\nEl1te Spr1nt Athlet1cs is working toward WCAG 2.2 Level AA so the website can be used by as many people as possible.\n\nOur ongoing work includes:\n\n- Keyboard access and visible focus\n- Screen-reader support and meaningful alternative text\n- Browser zoom and responsive layouts\n- Reduced-motion support\n- Clear labels, instructions, validation, and error messages\n\n## Third-party services\n\nSquare and Printify provide parts of the payment and fulfillment experience. We do not control every part of those third-party services, but we will help identify a practical alternative when possible.\n\n## Report a barrier\n\nEmail [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com) with the page, device or browser, approximate time, and a description of the problem. Please do not include payment information or sensitive athlete information.\n\nWe will investigate and respond as soon as practical. Accessibility is an ongoing responsibility, and this statement will be updated as the website and its services change.");

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000010"),
                column: "Body",
                value: "Last updated: August 4, 2026\n\n## Using this website\n\nBy using this website, you agree to these terms and, when making a purchase, the Store Policy. Club schedules, programs, eligibility, prices, inventory, and other content may change as information is reviewed.\n\nStore buyers must be at least 18 years old and must provide accurate contact, payment, and fulfillment information.\n\n## Acceptable use\n\nDo not attempt unauthorized access, disrupt the service, impersonate another person, submit information you are not authorized to provide, use automated requests that place an unreasonable burden on the service, or use club or sponsor intellectual property without permission.\n\n## Content and third parties\n\nEl1te owns or is authorized to use the website's content and branding. Viewing the website does not grant permission to copy or reuse that material.\n\nSquare, Printify, delivery carriers, sponsors, and linked websites operate under their own terms and policies. We will correct known website errors where practical, but we cannot promise uninterrupted service or guarantee a third party's availability or performance.\n\n## Questions and updates\n\nThese limitations apply only to the extent permitted by law. We may update these terms as the website changes. Questions may be sent to [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com).");

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000011"),
                column: "Body",
                value: "Last updated: August 4, 2026\n\n## Prices, payment, and delivery\n\nPrices are in U.S. dollars. Square processes payment and calculates configured taxes. Card details are not stored by El1te.\n\nPrintify products include free standard shipping to valid United States addresses. International and expedited shipping are not available at launch. Club-stock products are delivered through an arranged practice or event handoff and are not shipped by staff. A mixed order is paid together but its items arrive separately.\n\n## Review and cancellation\n\nReview the address, size, color, and approved-design choices before payment. You may cancel the complete order and receive a full Square refund from the secure order-status page during the 30-minute production hold. After production release begins, cancellation is not guaranteed.\n\nCorrecting a delivery address after payment requires canceling within the hold window and placing a new order.\n\n## Returns and product problems\n\nPrintify items are made to order. We do not accept returns because the wrong size or color was selected or because the buyer changed their mind. Report a damaged, misprinted, or incorrect item within 30 days of delivery with the order reference and photographs so we can investigate and arrange an appropriate replacement or refund.\n\nUnworn, unwashed club-stock items in original condition may be returned or exchanged within 14 days of handoff, subject to available stock.\n\nRefunds return to the original Square payment method. Processing time after a refund is submitted depends on the buyer's financial institution.\n\n## Timing and available choices\n\nProduction and delivery dates are estimates. We will help investigate delayed or lost shipments but cannot guarantee carrier timelines.\n\nLaunch products offer only the listed size, garment-color, and approved-design choices. Free-form names, numbers, and custom artwork are not available.\n\n## Store support\n\nEmail [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com) with your order reference. Do not send card details by email.");
        }
    }
}
