using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace El1teSpr1ntTrack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PublishLaunchPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                columns: new[] { "Body", "Title" },
                values: new object[] { "Last updated: August 4, 2026\n\n## Information we collect\n\nWhen you contact El1te Spr1nt Athlet1cs, we collect the name, email address, optional phone number, inquiry details, and message you provide. Store checkout collects the adult buyer's name, email, phone number, delivery address when shipping is required, product selections, order history, fulfillment status, tracking, cancellations, and refunds.\n\n## Payments and fulfillment\n\nCard details are entered on Square and are never received or stored by El1te. Printify receives the customer and order information required to manufacture and deliver Printify items. Delivery carriers receive the information required to deliver packages.\n\nAzure hosts the application and supports transactional email. Authorized staff may access submitted information only for club operations, support, security, accounting, safety, and legal obligations.\n\n## Browser storage and analytics\n\nThe public cart stores only non-personal product configuration in the browser. Essential security and checkout-return cookies may be used. Public performance analytics are cookie-free and exclude names, emails, addresses, form contents, cart details, and Admin activity.\n\nWe do not sell or rent personal information.\n\n## Retention and security\n\nRecords are kept only as reasonably needed for fulfillment, accounting, disputes, safety, security, and legal obligations. We use reasonable safeguards, but no internet service can promise absolute security.\n\n## Children and families\n\nStore purchases must be made by an adult. A parent or guardian should submit information involving a youth athlete.\n\n## Your choices\n\nTo request access, correction, or deletion, email [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com). Some records may need to be retained for accounting, safety, dispute, or legal reasons.", "Privacy Policy" });

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                columns: new[] { "Body", "Title" },
                values: new object[] { "Last updated: August 4, 2026\n\n## Our commitment\n\nEl1te Spr1nt Athlet1cs is working toward WCAG 2.2 Level AA so the website can be used by as many people as possible.\n\nOur ongoing work includes:\n\n- Keyboard access and visible focus\n- Screen-reader support and meaningful alternative text\n- Browser zoom and responsive layouts\n- Reduced-motion support\n- Clear labels, instructions, validation, and error messages\n\n## Third-party services\n\nSquare and Printify provide parts of the payment and fulfillment experience. We do not control every part of those third-party services, but we will help identify a practical alternative when possible.\n\n## Report a barrier\n\nEmail [el1tespr1nt.athlet1cs@gmail.com](mailto:el1tespr1nt.athlet1cs@gmail.com) with the page, device or browser, approximate time, and a description of the problem. Please do not include payment information or sensitive athlete information.\n\nWe will investigate and respond as soon as practical. Accessibility is an ongoing responsibility, and this statement will be updated as the website and its services change.", "Accessibility Statement" });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                columns: new[] { "Body", "Title" },
                values: new object[] { "We collect only the information needed to respond to inquiries, manage authorized club content, and operate approved services. Public performance analytics are configured without cookies or user identifiers. We do not sell personal information. Authorized staff may access submitted information only for club operations, support, safety, and legal obligations. Contact the club to ask about access, correction, or removal. This draft must be approved by the organization before public launch.", "Privacy" });

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                columns: new[] { "Body", "Title" },
                values: new object[] { "El1te Spr1nt Athlet1cs aims to provide a website that works with keyboards, screen readers, zoom, reduced motion, and common mobile devices. If you encounter a barrier, contact the club with the page, approximate time, and a description of the problem so staff can investigate and provide an alternative. This draft must be approved by the organization before public launch.", "Accessibility" });

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000010"),
                column: "Body",
                value: "This website provides club information and administrative tools. Content may change as schedules, programs, eligibility, and availability are reviewed. Do not misuse the site, attempt unauthorized access, or submit information you are not authorized to provide. External services and links have their own terms. This draft is factual operational guidance, not legal advice, and must be approved before public launch.");

            migrationBuilder.UpdateData(
                table: "ContentBlocks",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000011"),
                column: "Body",
                value: "The merchandise shop is currently a preview and does not accept payment. Before launch, final prices, tax, availability, handoff arrangements, customization review, cancellations, returns, and refunds will be shown before checkout. Card details will be handled by Square and not stored by El1te. This draft must be updated and approved before payments are enabled.");
        }
    }
}
