import { describe, expect, it } from "vitest";
import { isAllowedAdminMutation } from "../lib/admin/mutation-policy";
import type { CoachWriteRequest, ContentBlockWriteRequest, EventWriteRequest, FaqWriteRequest, SiteSettingsWriteRequest, SponsorWriteRequest } from "../lib/admin/types";
import { buildListQuery, validateCoach, validateContentBlock, validateEvent, validateFaq, validateSiteSettings, validateSponsor } from "../lib/admin/validation";

const id = "c6ca4e2a-0244-4f9d-9af6-95bbc65ca612";

describe("Phase 7 mutation boundary", () => {
  it.each([
    [["events"], "POST"], [["coaches", id], "PUT"], [["content-blocks", id], "DELETE"],
    [["site-settings"], "PUT"], [["contact-submissions", id, "status"], "PUT"], [["contact-submissions", id], "DELETE"]
  ] as const)("allows supported operation %#", (path, method) => expect(isAllowedAdminMutation([...path], method)).toBe(true));

  it.each([
    [["users"], "POST"], [["site-settings"], "POST"], [["contact-submissions"], "POST"],
    [["events", "not-a-guid"], "DELETE"], [["contact-submissions", id], "PUT"]
  ] as const)("rejects unsupported operation %#", (path, method) => expect(isAllowedAdminMutation([...path], method)).toBe(false));
});

describe("Phase 7 filters and validation", () => {
  it("preserves supported filters and normalizes pagination", () => expect(buildListQuery({ search: " meet ", eventType: "Meet", isPublished: "true", ignored: "x", page: "0" }, ["search", "eventType", "isPublished"])).toBe("search=meet&eventType=Meet&isPublished=true&page=1&pageSize=20"));

  it("rejects an event ending before it starts", () => { const input: EventWriteRequest = { title:"Meet",eventType:"Meet",startDateTimeUtc:"2026-07-02T14:00:00Z",endDateTimeUtc:"2026-07-02T13:00:00Z",locationName:"Track",address:null,description:"Race",registrationUrl:null,imageUrl:null,isFeatured:false,isPublished:false }; expect(validateEvent(input).endDateTimeUtc).toBeDefined(); });

  it("requires a valid email before publishing coach email", () => { const input: CoachWriteRequest = { firstName:"A",lastName:"Coach",role:"Head coach",bio:"Bio",imageUrl:null,email:null,isEmailPublic:true,displayOrder:0,isActive:true }; expect(validateCoach(input).email).toBeDefined(); input.email="invalid"; expect(validateCoach(input).email).toBeDefined(); });

  it("validates sponsor URLs and display order", () => { const input: SponsorWriteRequest = { name:"Partner",tier:"Gold",logoUrl:null,websiteUrl:"javascript:bad",description:null,displayOrder:-1,isActive:true }; const errors=validateSponsor(input); expect(errors.websiteUrl).toBeDefined(); expect(errors.displayOrder).toBeDefined(); });

  it("requires FAQ content and nonnegative ordering", () => { const input: FaqWriteRequest = { question:"",answer:"",category:"",displayOrder:-1,isActive:true }; expect(Object.keys(validateFaq(input))).toEqual(expect.arrayContaining(["question","answer","category","displayOrder"])); });

  it("requires content keys and validates CTA URLs", () => { const input: ContentBlockWriteRequest = { key:"",title:"Title",summary:null,body:"Body",imageUrl:null,ctaText:"Go",ctaUrl:"bad",displayOrder:0,isPublished:false }; const errors=validateContentBlock(input); expect(errors.key).toBeDefined(); expect(errors.ctaUrl).toBeDefined(); });

  it("validates singleton site settings without adding unsupported fields", () => { const input: SiteSettingsWriteRequest = { clubName:"Club",slogan:"Fast",contactEmail:"bad",phoneNumber:null,addressLine1:null,addressLine2:null,city:null,state:null,zipCode:null,facebookUrl:"bad",instagramUrl:null,youtubeUrl:null,primaryCtaText:"Join",primaryCtaUrl:"/join",secondaryCtaText:"Learn",secondaryCtaUrl:"/about",logoUrl:null }; const errors=validateSiteSettings(input); expect(errors.contactEmail).toBeDefined(); expect(errors.facebookUrl).toBeDefined(); expect(errors.primaryCtaUrl).toBeUndefined(); });
});
