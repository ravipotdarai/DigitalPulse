import { ChevronDown20Regular } from "@fluentui/react-icons";
import { motion } from "framer-motion";
import { useEffect, useState } from "react";
import { NavLink } from "react-router-dom";
import { BrandMark } from "../design/BrandMark";
import { useMotionTiming } from "../design/motion";
import { NAV_GROUPS, branchIsOpen, groupIsOn, itemIsOn, navHref } from "./navigation";

export function NavRail({
  pathname,
  search,
  collapsed
}: {
  pathname: string;
  search: string;
  collapsed: boolean;
}) {
  const { reduce, base, ease } = useMotionTiming();
  const selected = NAV_GROUPS.find((group) => groupIsOn(group, pathname, search))?.label ?? NAV_GROUPS[0].label;
  const [openLabel, setOpenLabel] = useState(selected);

  useEffect(() => {
    setOpenLabel(selected);
  }, [selected]);

  return (
    <nav className="os-nav-groups">
      {NAV_GROUPS.map((group) => {
        const GroupIcon = group.icon;
        const current = group.label === selected;
        const expanded = collapsed || openLabel === group.label;
        return (
          <div className={current ? "os-nav-group is-current" : "os-nav-group"} key={group.label}>
            <button
              type="button"
              className={current ? "os-nav-toggle is-on" : "os-nav-toggle"}
              aria-expanded={expanded}
              onClick={() => setOpenLabel((currentOpen) => (currentOpen === group.label ? selected : group.label))}
            >
              <GroupIcon aria-hidden="true" />
              <span>{group.label}</span>
              <motion.span
                className="os-nav-chevron"
                aria-hidden="true"
                initial={false}
                animate={{ rotate: expanded ? 180 : 0 }}
                transition={{ duration: reduce ? 0 : base, ease }}
              >
                <ChevronDown20Regular />
              </motion.span>
            </button>
            <div className={expanded ? "os-nav-items is-open" : "os-nav-items"}>
              <div className="os-nav-items-inner">
                {group.items.map((item) => {
                  const Icon = item.icon;
                  const on = itemIsOn(item, pathname, search);
                  const kidsOpen = !collapsed && expanded && branchIsOpen(item, pathname, search);
                  return (
                    <div className="os-nav-branch" key={`${item.to}:${item.search ?? item.label}`}>
                      <NavLink
                        to={navHref(item)}
                        end={item.end}
                        className={on ? "os-nav-link is-on" : "os-nav-link"}
                        aria-current={on ? "page" : undefined}
                        title={collapsed ? item.label : undefined}
                        tabIndex={expanded ? undefined : -1}
                      >
                        {item.brand ? <BrandMark code={item.brand} name={item.label} className="os-nav-brand" /> : <Icon aria-hidden="true" />}
                        <span>{item.label}</span>
                      </NavLink>
                      {kidsOpen && item.children ? (
                        <div className="os-nav-kids">
                          {item.children.map((child) => {
                            const ChildIcon = child.icon;
                            const childOn = itemIsOn(child, pathname, search);
                            return (
                              <NavLink
                                key={`${child.to}:${child.search ?? child.label}`}
                                to={navHref(child)}
                                className={childOn ? "os-nav-link os-nav-child is-on" : "os-nav-link os-nav-child"}
                                aria-current={childOn ? "page" : undefined}
                              >
                                <ChildIcon aria-hidden="true" />
                                <span>{child.label}</span>
                              </NavLink>
                            );
                          })}
                        </div>
                      ) : null}
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        );
      })}
    </nav>
  );
}
